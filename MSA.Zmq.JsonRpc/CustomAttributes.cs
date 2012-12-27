using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MSA.Zmq.JsonRpc
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class JsonRpcServiceHandlerAttribute : Attribute 
    {
        public JsonRpcServiceHandlerAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class JsonRpcMethodAttribute : Attribute
    {
        public JsonRpcMethodAttribute() : this(false, false, String.Empty, String.Empty) { }
        public JsonRpcMethodAttribute(bool authorize) : this(authorize, false, String.Empty, String.Empty) { }
        public JsonRpcMethodAttribute(bool authorize, bool logCall, string roles, string description)
        {
            Authorize = authorize;
            LogCall = logCall;
            RequiredRoles = roles;
            Description = description;
        }

        public bool Authorize { get; set; }
        public bool LogCall { get; set; }
        public string RequiredRoles { get; set; }
        public string Description { get; set; }
    }

}
