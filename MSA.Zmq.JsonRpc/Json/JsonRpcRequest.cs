using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public sealed class JsonRpcRequest
    {
        public JsonRpcRequest()
        {
            JsonRpc = "2.0";
            Token = null;
        }

        public string JsonRpc { get; private set; }
        public object[] Params { get; set; }
        public string Method { get; set; }
        public int Id { get; set; }
        public string Token { get; set; }
    }


    public sealed class JsonRpcRequestHeader
    {
        public JsonRpcRequestHeader(){}

        public string User { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public string IPAddress { get; set; }
        public string HostName { get; set; }
    }
}
