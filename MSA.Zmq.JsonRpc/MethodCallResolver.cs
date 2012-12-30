using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;

namespace MSA.Zmq.JsonRpc
{
    internal class MethodCallResolver
    {
        private IDictionary<string, RuntimeMethodHandle> _callableMethods;
        private IDictionary<string, Models.TaskDescriptor> _taskDescriptors;
        private IDictionary<string, JsonRpcMethodAttribute> _methodAttributeCache;

        private object _handler;
        private string _endpointPrefix;

        public MethodCallResolver(object handler, string endpointPrefix)
        {
            _handler = handler;
            _endpointPrefix = endpointPrefix;
            _methodAttributeCache = new Dictionary<string, JsonRpcMethodAttribute>();

            LoadFromHandler(_handler);
        }

        private string WrapException(int id, int code, string message, Exception ex)
        {
            return JsonRpcResponse.CreateJsonError(id, code, message, ex.StackTrace);
        }

        public bool HasMethod(string methodName)
        {
            return _callableMethods.ContainsKey(methodName);
        }

        private JsonRpcMethodAttribute FetchOrCacheMethodAttribute(MethodBase mi)
        {
            var key = !String.IsNullOrEmpty(_endpointPrefix) ? (_endpointPrefix + mi.Name) : mi.Name;
            if (!_methodAttributeCache.ContainsKey(key))
            {
                var rpcAttr = mi.GetCustomAttributes(typeof(JsonRpcMethodAttribute), false)[0] as JsonRpcMethodAttribute;
                _methodAttributeCache.Add(key, rpcAttr);
            }

            return _methodAttributeCache[key];
        }

        public string CallMethod(JsonRpcRequestHeader requestHeader, JsonRpcRequest request, Action<string> methodAuthorizer, Action methodLogger )
        {
            var responseJson = String.Empty;
            var authorizeMethodCall = false;
            var logMethodCall = false;
            var requiredRoles = String.Empty;

            // NOTES: Translate any exception to JsonRpcException
            try
            {
                try
                {
                    if (_callableMethods[request.Method] != null)
                    {
                        // NOTES: request.Method is combined key not just the bare method name
                        var mi = MethodInfo.GetMethodFromHandle(_callableMethods[request.Method]);

                        // Collects the arguments
                        var methodParams = new List<object>();
                        foreach (var pi in mi.GetParameters())
                        {
                            if (pi.ParameterType.IsPrimitive || pi.ParameterType == typeof(String) || pi.ParameterType.IsValueType)
                            {
                                methodParams.Add(request.Params[pi.Position]);
                            }
                            else
                            {
                                var paramObj = JsonConvert.DeserializeObject(request.Params[pi.Position].ToString(), pi.ParameterType);
                                methodParams.Add(paramObj);
                            }
                        }

                        // No need to check attribute type that has been done during loading
                        var attr = FetchOrCacheMethodAttribute(mi);
                        logMethodCall = attr.LogCall;
                        authorizeMethodCall = attr.Authorize;
                        requiredRoles = attr.RequiredRoles;

                        var td = _taskDescriptors[request.Method];
                        td.LastInvokedBy = requestHeader.User;
                        td.LastInvoked = DateTime.UtcNow;

                        // Authorize when demanded in attribute
                        if (authorizeMethodCall)
                            methodAuthorizer(requiredRoles);

                        var ret = mi.Invoke(_handler, methodParams.ToArray());

                        // Log invocation when demanded in attribute
                        if (logMethodCall)
                            methodLogger();

                        responseJson = JsonRpcResponse.CreateJsonResponse(request.Id, ret);
                    }
                    else
                    {
                        throw new MethodNotFoundException(String.Format("Method: {0} not found", request.Method));
                    }
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidParametersException(ex.Message);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                        throw new ServerErrorException(ex.InnerException.Message);

                    throw new ServerErrorException(ex.Message);
                }
            }
            catch (JsonRpcException ex)
            {
                if (logMethodCall && methodLogger != null)
                {
                    methodLogger();
                }

                responseJson = WrapException(request.Id, ex.Code, ex.Message, ex);
            }

            return responseJson;
        }

        public IList<Models.TaskDescriptor> GetAvailableTasks()
        {
            return _taskDescriptors.Values.ToList();
        }

        internal void LoadFromHandler(object handler)
        {
            if (_callableMethods == null)
            {
                _callableMethods = new Dictionary<string, RuntimeMethodHandle>();
            }

            if (_taskDescriptors == null)
            {
                _taskDescriptors = new Dictionary<string, Models.TaskDescriptor>();
            }

            var methods = handler.GetType().GetMethods(BindingFlags.IgnoreCase | BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance);
            foreach (var m in methods)
            {
                var attributes = m.GetCustomAttributes(typeof(JsonRpcMethodAttribute), true);
                if (attributes != null && attributes.Length > 0)
                {
                    var key = !String.IsNullOrEmpty(_endpointPrefix) ? (_endpointPrefix + m.Name) : m.Name;
                    _callableMethods.Add(key, m.MethodHandle);

                    var rpcAttr = attributes[0] as JsonRpcMethodAttribute;
                    _taskDescriptors.Add(key, new Models.TaskDescriptor() { 
                        Name = m.Name, 
                        Description = rpcAttr.Description, 
                        RequiredRoles = rpcAttr.RequiredRoles,
                        LogCall = rpcAttr.LogCall,
                        Authorize = rpcAttr.Authorize
                    });
                }
            }
        }
    }
}
