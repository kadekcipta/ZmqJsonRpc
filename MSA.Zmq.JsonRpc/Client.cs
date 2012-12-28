using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using ZeroMQ;

namespace MSA.Zmq.JsonRpc
{
    public enum MethodCallState
    {
        Error = -1,
        Processing = 0,
        Success = 1
    }

    public enum ClientMode
    {
        Rpc,
        Push,
        Subscriber
    }

    public delegate void ErrorEventHandler(object sender, JsonRpcException ex);
    public delegate void BeforeSendRequestHandler(object sender, JsonRpcRequest request);

    internal sealed class TaskItem
    {
        public string CommandRequest { get; set; }
        public Action<string> ResultProcessor { get; set; }
    }

    public class Client : JsonRpcZmqServiceBase
    {
        const int DEF_CONNECTION_TIMEOUT = 5000; // in milliseconds
        const int MAX_CALL_QUEUE = 10000;

        private uint _commandPort;
        private string _address;
        private string _userName;
        private string _password;
        private string _ipAddress;
        private string _hostName;
        private int _connectionTimeout;
        private Queue<TaskItem> _callQueue;
        private object _queueLock;
        private Thread _queueProcessorThread;
        private bool _queueProcessing;
        private AutoResetEvent _processingLock;
        private ClientMode _mode;
        private Random _random;

        public event ErrorEventHandler ServiceError;
        public event BeforeSendRequestHandler BeforeSendRequest;

        public static Client Connect(string address, uint servicePort, string userName, string password, ClientMode mode)
        {
            return new Client(address, servicePort, userName, password, mode);
        }

        public static Client Connect(string address, uint servicePort, ClientMode mode)
        {
            return Connect(address, servicePort, String.Empty, String.Empty, mode);
        }

        private Client(string address, uint servicePort, string userName, string password, ClientMode mode)
        {
            _random = new Random(1);
            _mode = mode;
            _queueProcessorThread = null;
            _queueLock = new Object();
            _processingLock = new AutoResetEvent(true);
            _callQueue = new Queue<TaskItem>();
            _queueProcessing = false;
            _address = address;
            _commandPort = servicePort;
            _userName = userName;
            _password = password;

            if (String.IsNullOrEmpty(_userName) && String.IsNullOrEmpty(_password))
            {
                _userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }

            _connectionTimeout = DEF_CONNECTION_TIMEOUT;
            var timeoutValue = System.Configuration.ConfigurationManager.AppSettings["ZMQ_ConnectionTimeout"];
            if (!String.IsNullOrEmpty(timeoutValue))
            {
                _connectionTimeout = Convert.ToInt32(timeoutValue);
            }

            _hostName = System.Net.Dns.GetHostName();
            var ips = System.Net.Dns.GetHostAddresses(_hostName);
            if (ips != null && ips.Length > 0)
            {
                _ipAddress = ips[0].ToString();
            }
        }

        public void Push(string channel, string message)
        {
            throw new NotImplementedException();
        }

        public void Subscribe(string channel, Action<string> callback)
        {
            throw new NotImplementedException();
        }

        public void CallMethodAsync(string methodName, params object[] args)
        {
            CallMethodAsync(methodName, null, args);
        }

        public void CallMethodAsync(string methodName, Action<MethodCallState> stateCallback, params object[] args)
        {
            CallMethodAsync<JsonRpcResponse>(methodName, null, stateCallback, args);
        }

        public void CallMethodAsync<T>(string methodName, Action<T> callback, params object[] args)
        {
            CallMethodAsync<T>(methodName, callback, null, args);
        }

        public void CallMethodAsync<T>(string methodName, Action<T> callback, Action<MethodCallState> stateCallback, params object[] args)
        {

            var asyncOp = AsyncOperationManager.CreateOperation(methodName);
            OnMethodCallState(stateCallback, MethodCallState.Processing);
            SendRequestAsync<JsonRpcResponse>((response) =>
            {
                if (response.Error != null)
                {
                    var e = JsonConvert.DeserializeObject<JsonRpcError>(response.Error.ToString());
                    OnError(new JsonRpcException(e.Code, e.Message), asyncOp);
                    OnMethodCallState(stateCallback, MethodCallState.Error);
                }
                else
                {
                    if (response.Result != null)
                    {
                        var callbackParams = IsTypeNotCustomObject(typeof(T)) ? (T)(object)response.Result : JsonConvert.DeserializeObject<T>(response.Result.ToString());
                        if (callback != null)
                            asyncOp.Post((state) => callback((T)state), callbackParams);
                    }

                    OnMethodCallState(stateCallback, MethodCallState.Success);
                }
            }, methodName, args);
        }

        public override void Stop()
        {
            _queueProcessing = false;
            _processingLock.Set();
        }

        private bool IsTypeNotCustomObject(Type type)
        {
            return (type.IsPrimitive || type.IsValueType || type == typeof(string));
        }

        private void OnMethodCallState(Action<MethodCallState> stateCallback, MethodCallState state)
        {
            if (stateCallback != null)
            {
                stateCallback(state);
            }
        }

        private void EnsureQueueProcessorRunning()
        {
            if (_queueProcessorThread == null)
            {
                _queueProcessing = true;
                _queueProcessorThread = new Thread(new ThreadStart(() => {
                    string response = String.Empty;
                    using (ZmqSocket socket = ServiceContext.CreateSocket(SocketType.REQ))
                    {
                        socket.Linger = TimeSpan.FromSeconds(0);
                        socket.Connect(String.Format("tcp://{0}:{1}", this._address, this._commandPort));

                        try
                        {
                            var requestHeader = new JsonRpc.JsonRpcRequestHeader();
                            if (!String.IsNullOrEmpty(_userName))
                                requestHeader.User = _userName;

                            requestHeader.Password = _password;
                            requestHeader.IPAddress = _ipAddress;
                            requestHeader.HostName = _hostName;

                            while (_queueProcessing)
                            {
                                TaskItem taskItem = null;
                                lock (_queueLock)
                                {
                                    if (_callQueue.Count > 0)
                                        taskItem = _callQueue.Dequeue();
                                }

                                if (taskItem != null)
                                {
                                    socket.SendMore(String.Empty, Encoding.UTF8);
                                    socket.SendMore(JsonConvert.SerializeObject(requestHeader), Encoding.UTF8);
                                    socket.Send(taskItem.CommandRequest, Encoding.UTF8);
                                    var result = socket.Receive(Encoding.UTF8, TimeSpan.FromSeconds(DEF_CONNECTION_TIMEOUT)); // result is string
                                    taskItem.ResultProcessor(result);
                                }
                                else
                                {
                                    _processingLock.WaitOne();
                                }
                            }
                            
                            _queueProcessorThread = null;
                        }
                        catch (ZeroMQ.ZmqException ex)
                        {
                            _queueProcessorThread = null;
                            LogError(ex);
                        }
                    }
                }));

                if (_queueProcessorThread != null)
                {
                    _queueProcessorThread.IsBackground = true;
                    _queueProcessorThread.Start();
                }
            }
        }

        private void EnqueueTask(TaskItem taskItem)
        {
            lock (_queueLock)
            {
                if (_callQueue.Count < MAX_CALL_QUEUE)
                {
                    _callQueue.Enqueue(taskItem);
                    _processingLock.Set();
                }
            }

            EnsureQueueProcessorRunning();
        }

        public T CallMethod<T>(string methodName, params object[] args)
        {
            var result = default(T);

            SendRequest<JsonRpcResponse>((response) =>
            {
                if (response.Error != null)
                {
                    var e = JsonConvert.DeserializeObject<JsonRpcError>(response.Error.ToString());
                    OnError(new JsonRpcException(e.Code, e.Message), null);
                }
                else
                {
                    result = IsTypeNotCustomObject(typeof(T)) ? (T)(object)response.Result : JsonConvert.DeserializeObject<T>(response.Result.ToString());
                }
            }, methodName, args);

            return result;
        }

        public void CallMethod(string methodName, params object[] args)
        {
            SendRequest<JsonRpcResponse>((response) =>
            {
                if (response.Error != null)
                {
                    var e = JsonConvert.DeserializeObject<JsonRpcError>(response.Error.ToString());
                    OnError(new JsonRpcException(e.Code, e.Message), null);
                }
            }, methodName, args);
        }

        protected void SendRequest<T>(Action<T> callback, string methodName, params object[] args)
        {
            if (_mode != ClientMode.Rpc)
                throw new InvalidOperationException("Can only be called in Rpc mode");

            var request = new JsonRpcRequest()
            {
                Id = 1,
                Params = args,
                Method = methodName
            };

            var commandRequest = JsonConvert.SerializeObject(request);

            using (ZmqSocket socket = ServiceContext.CreateSocket(SocketType.REQ))
            {
                socket.Linger = TimeSpan.FromSeconds(0);
                socket.Connect(String.Format("tcp://{0}:{1}", this._address, this._commandPort));

                try
                {
                    var requestHeader = new JsonRpc.JsonRpcRequestHeader();
                    if (!String.IsNullOrEmpty(_userName))
                        requestHeader.User = _userName;

                    requestHeader.Password = _password;
                    requestHeader.IPAddress = _ipAddress;
                    requestHeader.HostName = _hostName;
                    socket.SendMore(String.Empty, Encoding.UTF8);
                    socket.SendMore(JsonConvert.SerializeObject(requestHeader), Encoding.UTF8);
                    socket.Send(commandRequest, Encoding.UTF8);
                    var result = socket.Receive(Encoding.UTF8, TimeSpan.FromSeconds(DEF_CONNECTION_TIMEOUT));
                    if (callback != null)
                    {
                        if (result != null)
                            callback(JsonConvert.DeserializeObject<T>(result));
                        else
                        {
                            result = JsonRpcResponse.CreateJsonError(-1, -1, "Connection is timeout", "");
                            callback(JsonConvert.DeserializeObject<T>(result));
                        }                        
                    }
                }
                catch (ZeroMQ.ZmqException ex)
                {
                    LogError(ex);
                }
            }
        }

        protected void SendRequestAsync<T>(Action<T> callback, string methodName, params object[] args)
        {
            if (_mode != ClientMode.Rpc)
                throw new InvalidOperationException("Can only be called in Rpc mode");

            var request = new JsonRpcRequest()
            {
                Id = _random.Next(1, MAX_CALL_QUEUE),
                Params = args,
                Method = methodName
            };

            var commandRequest = JsonConvert.SerializeObject(request);
            string result = null;
            var taskItem = new TaskItem()
            {
                CommandRequest = commandRequest,
                ResultProcessor = (res) =>
                {
                    if (res != null)
                        callback(JsonConvert.DeserializeObject<T>(res));
                    else
                    {
                        result = JsonRpcResponse.CreateJsonError(-1, -1, "Connection is timeout", "");
                        callback(JsonConvert.DeserializeObject<T>(res));
                    }
                }
            };

            EnqueueTask(taskItem);
        }

        private void OnBeforeSendRequest(JsonRpcRequest request, AsyncOperation asyncOp)
        {
            if (BeforeSendRequest != null)
            {
                asyncOp.Post((state) => BeforeSendRequest(this, (JsonRpcRequest)state), request);
            }
        }

        private void OnError(JsonRpcException ex, AsyncOperation asyncOp)
        {
            var _asyncOp = asyncOp;
            if (_asyncOp == null)
                _asyncOp = AsyncOperationManager.CreateOperation(null);

            if (ServiceError != null)
            {
                LogError(ex);
                _asyncOp.Post((state) => ServiceError(this, (JsonRpcException)state), ex);
            }
            else
            {
                throw ex;
            }
        }
    }
}
