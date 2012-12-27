using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public interface IRequestProcessor
    {
        bool ProcessRequest(JsonRpcRequestHeader requestHeader, JsonRpcRequest request); 
    }
}
