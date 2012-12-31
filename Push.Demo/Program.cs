using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSA.Zmq.JsonRpc;

namespace Push.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var client = Client.CreatePushContext("tcp://127.0.0.1:3002"))
            {
                while (true)
                {
                    Console.Write("> ");
                    var message = Console.ReadLine();
                    if (!String.IsNullOrEmpty(message))
                        client.Push("update", message);
                }
                
            }
        }
    }
}
