using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc.BuiltinsTasks
{
    [JsonRpcServiceHandler]
    public class CmdHandler
    {
        [JsonRpcMethod]
        public string Cmd(string command)
        {
            using (var cmd = new Process())
            {
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.Arguments = "/C " + command;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;
                if (cmd.Start())
                {
                    using (var reader = cmd.StandardOutput)
                    {
                        return reader.ReadToEnd();
                    }
                }

                return "";
            }
        }
    }
}
