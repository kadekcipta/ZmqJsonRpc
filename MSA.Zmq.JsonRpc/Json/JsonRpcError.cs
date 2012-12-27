using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public sealed class JsonRpcError
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
    }
}

