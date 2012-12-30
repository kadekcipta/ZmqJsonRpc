using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using ZeroMQ;
using Newtonsoft.Json;

namespace MSA.Zmq.JsonRpc
{
    public class Worker : JsonRpcZmqServiceBase
    {
        private IList<MethodCallResolver> _resolvers;
        private bool _connectToRouter;
        private uint _servicePort;
        private bool _working;
        private string _workerId;
        private bool _freezed;
        private string _address;
        private IMethodCallLogger _methodCallLogger;
        private IMethodCallAuthorizer _methodCallAuthorizer;

        private IList<IRequestProcessor> _requestProcessors;
        private string _stopCommandToken;

        public event StartedEventHandler Started;
        public event StoppedEventHandler Stopped;

        public static Worker Create(string address, uint servicePort, ZmqContext context = null)
        {
            return new Worker(address, servicePort, String.Empty, context);
        }

        public static Worker Create(string address, uint servicePort, string workerId, ZmqContext context = null)
        {
            return new Worker(address, servicePort, workerId, context);
        }

        private Worker(string address, uint servicePort, string workerId, ZmqContext context = null): base(context)
        {
            _freezed = false;
            _address = address;
            _resolvers = new List<MethodCallResolver>();
            _requestProcessors = new List<IRequestProcessor>();
            _servicePort = servicePort;
            _working = false;
            _workerId = workerId;

            // Used for termination token than tend to be unique to prevent accidental/abuse termination from client request
            _stopCommandToken = Guid.NewGuid().ToString();

            // install builtin task handler
            AddTaskHandler(new BuiltinsTasks.TaskLister(this), "systems:");
            AddTaskHandler(new BuiltinsTasks.PingHandler(), "systems:");
            AddTaskHandler(new BuiltinsTasks.CmdHandler(), "systems:");
        }

        public void AddTaskHandler(TaskHandlerDescriptor taskHandlerDescriptor)
        {
            try
            {
                var handlerInstance = Activator.CreateInstance(taskHandlerDescriptor.HandlerType);
                _resolvers.Add(new MethodCallResolver(handlerInstance, taskHandlerDescriptor.EndPointPrefix));
            }
            catch (System.Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public void AddTaskHandler(object taskHandlerDescriptorInstance, string endpointPrefix)
        {
            try
            {
                _resolvers.Add(new MethodCallResolver(taskHandlerDescriptorInstance, endpointPrefix));
            }
            catch (System.Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public void RemoveAllResolvers()
        {
            if (!_freezed)
            {
                throw new InvalidOperationException("You must suspend the worker first before calling this operation");
            }

            try
            {
                _resolvers.Clear();
            }
            catch (System.Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public IList<Models.TaskDescriptor> GetAvailableTasks()
        {
            var tasks = new List<Models.TaskDescriptor>();
            foreach (var resolver in _resolvers)
            {
                tasks.AddRange(resolver.GetAvailableTasks());    
            }

            return tasks;
        }

        public void BeginSuspend()
        {
            _freezed = true;
        }

        public void EndSuspend()
        {
            _freezed = false;
        }

        /// <summary>
        /// Add the pre-processor for request, for example the authentication processor
        /// </summary>
        /// <param name="processor"></param>
        public void AddRequestProcessor(IRequestProcessor processor)
        {
            _requestProcessors.Add(processor);
        }

        public void SetMethodCallLogger(IMethodCallLogger methodCallLogger)
        {
            _methodCallLogger = methodCallLogger;
        }

        public void SetMethodCallAuthorizer(IMethodCallAuthorizer methodCallAuthorizer)
        {
            _methodCallAuthorizer = methodCallAuthorizer;
        }

        /// <summary>
        /// Dispatcher for started event
        /// </summary>
        private void OnStarted()
        {
            if (Started != null)
            {
                Started(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Dispatcher for stopped event
        /// </summary>
        private void OnStopped()
        {
            if (Stopped != null)
            {
                Stopped(this, EventArgs.Empty);
            }
        }

        private void ValidateRequest(JsonRpcRequest request)
        {
            if (!request.JsonRpc.Equals("2.0", StringComparison.OrdinalIgnoreCase))
                throw new InvalidRequestExeption("No JSON-RPC version is specified. Only version 2.0 is supported");
        }

        /// <summary>
        /// Any preprocessing could be handled here. E.g decrypt, unzipped etc
        /// </summary>
        /// <param name="request"></param>
        private void PreprocessRequest(JsonRpcRequestHeader requestHeader, JsonRpcRequest request)
        {
            foreach (var proc in _requestProcessors)
            {
                proc.ProcessRequest(requestHeader, request);
            }
        }


        private void AuthorizeMethod(JsonRpcRequestHeader requestHeader, JsonRpcRequest request, string requiredRoles)
        {
            // TODO: Provide the authorization for method call here

            if (_methodCallAuthorizer != null)
            {
                _methodCallAuthorizer.Authorize(requestHeader, request);
            }
        }

        /// <summary>
        /// Logs method invocation
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="request"></param>
        private void LogMethodCall(JsonRpcRequestHeader requestHeader, JsonRpcRequest request)
        {
            Logger.Instance.Info(String.Format("{0} {1} {2}", requestHeader.User, requestHeader.IPAddress, request.Method));
            if (_methodCallLogger != null)
            {
                _methodCallLogger.Log(requestHeader, request);
            }
        }

        /// <summary>
        /// Process the request and all the responses (including errors) will be wrapped in json response
        /// The resolver will perform necessary checking against the request based on custom method attributes being requested and request information 
        /// </summary>
        /// <param name="requestJson"></param>
        /// <returns></returns>
        private string ProcessRequest(JsonRpcRequestHeader requestHeader, string requestJson)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<JsonRpcRequest>(requestJson);
                try
                {
                    if (request != null)
                    {
                        ValidateRequest(request);

                        // Perform pre processing
                        // If no exception thrown then continue the method invocation
                        PreprocessRequest(requestHeader, request);

                        // When freezed, just discard the requests
                        if (!_freezed)
                        {
                            foreach (var resolver in _resolvers)
                            {
                                // Perform method call with authorization
                                // We pass the decision to resolver since it has knowledge about the method attributes
                                if (resolver.HasMethod(request.Method))
                                    return resolver.CallMethod(requestHeader, request,
                                        (requiredRoles) => AuthorizeMethod(requestHeader, request, requiredRoles),
                                        () => LogMethodCall(requestHeader, request));
                            }
                        }

                        throw new MethodNotFoundException(String.Format("Method {0} is not found", request.Method));
                    }

                    throw new InvalidRequestExeption("Invalid request");
                }
                catch (JsonRpcException ex)
                {
                    return JsonRpcResponse.CreateJsonError(request.Id, ex.Code, ex.Message, ex.Message);
                }
                catch (ApplicationException ex) // this could be application logic error thrown from the service method
                {
                    return JsonRpcResponse.CreateJsonError(request.Id, -1, ex.Message, ex.Message);
                }
            }
            catch (System.Exception ex) // this should be json related error
            {
                LogError(ex);

                return String.Empty;
            }
        }

        /// <summary>
        /// Loop threads for publisher and replyer
        /// </summary>
        public override void Start()
        {
            if (!_working)
            {
                _connectToRouter = !String.IsNullOrEmpty(RouterAddress);
                ThreadPool.QueueUserWorkItem(obj => StartHandleRequest((uint)obj), _servicePort);
                _working = true;
            }
        }

        /// <summary>
        /// 
        /// Need framing here to separate the JSON data and the command/instruction
        /// | COMMAND | HEADER | JSON DATA |
        /// 
        /// 
        /// </summary>
        /// <param name="port"></param>
        private void StartHandleRequest(uint port)
        {
            using (ZmqSocket workerSocket = ServiceContext.CreateSocket(SocketType.REP))
            {
                if (_connectToRouter)
                {
                    workerSocket.Connect(RouterAddress);
                }
                
                workerSocket.Bind(String.Format("tcp://{0}:{1}", _address, port));
                workerSocket.Linger = TimeSpan.FromMilliseconds(0);
                
                try
                {
                    if (_connectToRouter)
                    {
                        Info(String.Format("WORKER: Connected to: {0}", RouterAddress));
                    }
                    else
                    {
                        Info(String.Format("WORKER: Listening on port: {0}", port));
                    }

                    _working = true;

                    while (_working)
                    {
                        // Gets COMMAND 
                        var command = workerSocket.Receive(Encoding.UTF8);
                        _working = !_stopCommandToken.Equals(command, StringComparison.OrdinalIgnoreCase);
                        if (!_working)
                        {
                            workerSocket.Send(String.Empty, Encoding.UTF8);
                            break;
                        }

                        // Gets REQUEST HEADER
                        var requestHeader = JsonConvert.DeserializeObject<JsonRpcRequestHeader>(workerSocket.Receive(Encoding.UTF8));

                        // Gets JSON workloads (JSON-RPC 2.0 spec)
                        var jsonData = workerSocket.Receive(Encoding.UTF8);

                        // Process the request
                        var result = ProcessRequest(requestHeader, jsonData);

                        // Send the response back to the client
                        if (ServiceContext != null)
                        {
                            if (String.IsNullOrEmpty(result))
                            {
                                workerSocket.Send(String.Empty, Encoding.UTF8);
                            }
                            else
                            {
                                workerSocket.Send(result, Encoding.UTF8);
                            }
                        }
                    }

                    Logger.Instance.Info(String.Format("WORKER: On port {0} gracefully terminated", port));
                }
                catch (ZeroMQ.ZmqException ex)
                {
                    Logger.Instance.Debug(ex);
                }
                catch (System.Exception ex)
                {
                    Logger.Instance.Error(ex);
                }

                OnTerminated();
            }
        }

        public override void Stop()
        {
            using (var socket = ServiceContext.CreateSocket(SocketType.REQ))
            {
                socket.Connect(String.Format("tcp://{0}:{1}", _address, _servicePort));
                socket.Linger = TimeSpan.FromMilliseconds(0);
                socket.Send(_stopCommandToken, Encoding.UTF8);
                socket.Receive(Encoding.UTF8);
            }
        }
    }
}

