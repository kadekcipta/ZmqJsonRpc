using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace MSA.Zmq.JsonRpc
{
    public class JsonRpcResultProcessor: IResultProcessor
    {
        public void ProcessResult(string result, Action<JsonRpcResponse> callback)
        {
            JsonRpcResponse ret = null;
            if (!String.IsNullOrEmpty(result))
                ret = JsonConvert.DeserializeObject<JsonRpcResponse>(result);
            else
            {
                var jsonResult = JsonRpcResponse.CreateJsonError(-1, -1, "Returned empty result", "");
                ret = JsonConvert.DeserializeObject<JsonRpcResponse>(jsonResult);
            }

            if (String.IsNullOrEmpty(ret.JsonRpc) || ret.JsonRpc != "2.0")
            {
                ret = JsonConvert.DeserializeObject<JsonRpcResponse>(JsonRpcResponse.CreateJsonError(-1, -32600, "Invalid RPC response", ""));
            }

            if (callback != null)
                callback(ret);            
        }
    }
}
