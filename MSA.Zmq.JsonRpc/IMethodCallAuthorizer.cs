using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public interface IMethodCallAuthorizer
    {
        void Authorize(JsonRpc.JsonRpcRequestHeader requestHeader, JsonRpc.JsonRpcRequest request);
    }
}
