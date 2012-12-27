using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc.BuiltinsTasks
{
    [JsonRpcServiceHandler]
    public class PingHandler
    {
        public PingHandler()
        {
        }

        [JsonRpcMethod]
        public string Ping()
        {
            return "Pong";
        }
    }
}
