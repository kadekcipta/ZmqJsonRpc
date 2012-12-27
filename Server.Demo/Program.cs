using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSA.Zmq.JsonRpc;

namespace Server.Demo
{
    [JsonRpcServiceHandler]
    class SimpleTestHandler
    {
        [JsonRpcMethod(Authorize = true, LogCall = false, Description = "Just say hello", RequiredRoles = "admin, developer")]
        public string Echo(string message)
        {
            return message;
        }

        [JsonRpcMethod]
        public double AddNumber(double v1, double v2)
        {
            return v1 + v2;
        }

        [JsonRpcMethod]
        public void Dumb(string value)
        {
            Console.WriteLine(value);
        }

        [JsonRpcMethod]
        public DateTime EchoDate(DateTime value)
        {
            return value;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (var worker = Worker.Create(3001))
            {
                worker.AddTaskHandler(new TaskHandlerDescriptor(typeof(SimpleTestHandler), null));
                worker.Start();

                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
            }
        }
    }
}
