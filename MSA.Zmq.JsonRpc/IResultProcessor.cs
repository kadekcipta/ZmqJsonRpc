using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public interface IResultProcessor
    {
        void ProcessResult(string result, Action<JsonRpcResponse> callback);
    }
}
