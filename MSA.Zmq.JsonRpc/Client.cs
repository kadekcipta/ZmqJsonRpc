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

    internal class ContextManager
    {
        private static ZmqContext _context;
        private static int _refCount;

        static ContextManager()
        {
            _refCount = 0;
        }

        public static ZmqContext GetContext()
        {
            if (_context == null)
            {
                _context = ZmqContext.Create();
            }

            ++_refCount;

            return _context;
        }

        public static void ReleaseContext()
        {
            if (_refCount > 0 && --_refCount == 0)
            {
                _context.Dispose();
                _context = null;
            }
        }
    }

    public class ClientBase : IDisposable
    {
        private bool _disposed;
        public ClientBase()
        {
            _disposed = false;
            Context = ContextManager.GetContext();
        }

        ~ClientBase()
        {
            Dispose();
        }

        public ZmqContext Context
        {
            get;
            private set;
        }

        protected virtual void Destroy()
        {
        }

        protected void Info(string message)
        {
            Logger.Instance.Info(message);
        }

        protected void LogError(System.Exception ex)
        {
            Logger.Instance.Error(ex);
        }

        protected void Debug(string message)
        {
            Logger.Instance.Debug(message);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Destroy();
                ContextManager.ReleaseContext();
                _disposed = true;
            }
        }
    }

    public delegate void ErrorEventHandler(object sender, JsonRpcException ex);
    public delegate void BeforeSendRequestHandler(object sender, JsonRpcRequest request);

    internal sealed class TaskItem
    {
        public string CommandRequest { get; set; }
        public Action<string> ResultProcessor { get; set; }
    }

    public static class Client
    { 
        public static JsonRpcClient CreateJsonRpcContext(string userName, string password, params string[] serviceEndPoints)
        {
            return new JsonRpcClient(userName, password, serviceEndPoints);
        }

        public static JsonRpcClient CreateJsonRpcContext(params string[] serviceEndPoints)
        {
            return new JsonRpcClient(String.Empty, String.Empty, serviceEndPoints);
        }

        public static SubscriberClient CreateSubscriberContext(string serviceEndPoint)
        {
            return new SubscriberClient(serviceEndPoint);
        }

        public static PushClient CreatePushContext(string serviceEndPoint)
        {
            return new PushClient(serviceEndPoint);
        }
    }

    public sealed class JsonRpcClient: ClientBase
    {
        const int DEF_CONNECTION_TIMEOUT = 5000; // in milliseconds
        const int MAX_CALL_QUEUE = 10000;

        private IResultProcessor _resultProcessor;
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
        private Random _random;
        private string[] _serviceEndPoints;

        public event ErrorEventHandler ServiceError;
        public event BeforeSendRequestHandler BeforeSendRequest;

        internal JsonRpcClient(params string[] serviceEndPoints): this(String.Empty, String.Empty, serviceEndPoints) {}
        internal JsonRpcClient(string userName, string password, params string[] serviceEndPoints): base()
        {
            _serviceEndPoints = serviceEndPoints;
            _resultProcessor = new JsonRpcResultProcessor();
            _random = new Random(1);
            _queueProcessorThread = null;
            _queueLock = new Object();
            _processingLock = new AutoResetEvent(false);
            _callQueue = new Queue<TaskItem>();
            _queueProcessing = false;
            _userName = userName;
            _password = password;
            ThrowsExceptionOnEmptyResult = false;

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

        public bool ThrowsExceptionOnEmptyResult
        {
            get;
            set;
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
            SendRequestAsync((response) =>
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

        protected override void Destroy()
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

        /// <summary>
        /// This is just one way to process requests, it will somewhat locked for long running operation 
        /// since it's based on single thread for queue processing.
        /// We will examine one thread per request
        /// </summary>
        private void EnsureQueueProcessorRunning()
        {
            if (Context == null)
                return;

            if (_queueProcessorThread == null)
            {
                _queueProcessing = true;
                _queueProcessorThread = new Thread(new ThreadStart(() =>
                {
                    string response = String.Empty;
                    using (ZmqSocket socket = Context.CreateSocket(SocketType.REQ))
                    {
                        socket.Linger = TimeSpan.FromSeconds(0);
                        foreach (var endPoint in _serviceEndPoints)
                            socket.Connect(endPoint);

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
            EnsureQueueProcessorRunning();
            lock (_queueLock)
            {
                if (_callQueue.Count < MAX_CALL_QUEUE)
                {
                    _callQueue.Enqueue(taskItem);
                    _processingLock.Set();
                }
            }
        }

        public T CallMethod<T>(string methodName, params object[] args)
        {
            var result = default(T);

            SendRequest((response) =>
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
            SendRequest((response) =>
            {
                if (response.Error != null)
                {
                    var e = JsonConvert.DeserializeObject<JsonRpcError>(response.Error.ToString());
                    OnError(new JsonRpcException(e.Code, e.Message), null);
                }
            }, methodName, args);
        }

        private void SendRequest(Action<JsonRpcResponse> callback, string methodName, params object[] args)
        {
            var request = new JsonRpcRequest()
            {
                Id = 1,
                Params = args,
                Method = methodName
            };

            var commandRequest = JsonConvert.SerializeObject(request);

            using (ZmqSocket socket = Context.CreateSocket(SocketType.REQ))
            {
                socket.Linger = TimeSpan.FromSeconds(0);
                foreach (var endPoint in _serviceEndPoints)
                    socket.Connect(endPoint);

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
                    _resultProcessor.ProcessResult(result, (response) =>
                    {
                        if (callback != null)
                            callback(response);
                    });
                }
                catch (ZeroMQ.ZmqException ex)
                {
                    LogError(ex);
                }
            }
        }

        private void SendRequestAsync(Action<JsonRpcResponse> callback, string methodName, params object[] args)
        {
            var request = new JsonRpcRequest()
            {
                Id = _random.Next(1, MAX_CALL_QUEUE),
                Params = args,
                Method = methodName
            };

            var commandRequest = JsonConvert.SerializeObject(request);
            var taskItem = new TaskItem()
            {
                CommandRequest = commandRequest,
                ResultProcessor = (result) =>
                {
                    _resultProcessor.ProcessResult(result, (response) =>
                    {
                        if (callback != null)
                            callback(response);
                    });
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
                if (ThrowsExceptionOnEmptyResult)
                    throw ex;
            }
        }
    }

    public sealed class PushClient : ClientBase
    {
        private string _serviceEndPoint;
        private Thread _pushThread;
        private bool _pushing;
        private object _queueLock;
        private Queue<string> _messageQueue;

        internal PushClient(string serviceEndPoint)
        {
            _queueLock = new object();
            _messageQueue = new Queue<string>();
            _serviceEndPoint = serviceEndPoint;
        }

        protected override void Destroy()
        {
            _pushing = false;
        }

        private void EnsurePushThreadRunning()
        {
            if (_pushThread == null)
            {
                _pushThread = new Thread(new ThreadStart(() => {
                    _pushing = true;
                    using (var socket = Context.CreateSocket(SocketType.PUSH))
                    {
                        socket.Linger = TimeSpan.FromMilliseconds(0);
                        socket.SendHighWatermark = 100;
                        socket.Connect(_serviceEndPoint);

                        while (_pushing)
                        {
                            lock (_queueLock)
                            {
                                if (_messageQueue.Count > 0)
                                {
                                    socket.Send(_messageQueue.Dequeue(), Encoding.UTF8);
                                }
                            }
                        }

                        _pushThread = null;
                    }
                }));

                _pushThread.IsBackground = true;
                _pushThread.Start();
            }
        }

        public void Push(string prefix, string message)
        {
            lock (_queueLock)
            {
                _messageQueue.Enqueue(prefix + message);
            }

            EnsurePushThreadRunning();
        }
    }

    public sealed class SubscriberClient : ClientBase
    {
        private string _serviceEndPoint;
        private bool _receiving;

        internal SubscriberClient(string serviceEndPoint)
        {
            _receiving = true;
            _serviceEndPoint = serviceEndPoint;
        }

        protected override void Destroy()
        {
            _receiving = false;
        }

        private void DoThreadSafeCallback(AsyncOperation asyncOp, string message, Action<string> callback)
        {
            if (asyncOp != null)
            {
                asyncOp.Post((state) => callback((string)state), message);
            }
            else
            {
                callback(message);
            }
        }

        private void HandleSubscription(AsyncOperation asyncOp, string prefix, Action<string> callback)
        {
            using (var socket = Context.CreateSocket(SocketType.SUB))
            {
                var prefixData = Encoding.UTF8.GetBytes(prefix);
                socket.Subscribe(prefixData);
                socket.Connect(_serviceEndPoint);
                while (_receiving)
                {
                    var message = socket.Receive(Encoding.UTF8);
                    if (!String.IsNullOrEmpty(message) && callback != null)
                    {
                        message = message.Remove(0, prefix.Length);
                        DoThreadSafeCallback(asyncOp, message, callback);
                    }
                }
            }
        }

        private void CreateSubscriptionThread(AsyncOperation asyncOp, string prefix, Action<string> callback)
        {
            ThreadPool.QueueUserWorkItem(obj => {
                HandleSubscription(asyncOp, prefix, callback);
            });
        }

        public void Subscribe(string prefix, Action<string> callback)
        {
            var asyncOp = AsyncOperationManager.CreateOperation(null);
            CreateSubscriptionThread(asyncOp, prefix, callback);
        }
    }
}
