using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public interface IMethodCallLogger
    {
        void Log(JsonRpc.JsonRpcRequestHeader requestHeader, JsonRpc.JsonRpcRequest request);
    }
}
