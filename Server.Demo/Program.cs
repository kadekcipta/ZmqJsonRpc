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
        [JsonRpcMethod(Authorize = true, LogCall = false, Description = "Just echoing")]
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
            Console.WriteLine("RPC server and event publisher");
            using (var context = ZeroMQ.ZmqContext.Create())
            using (var worker = Worker.Create("127.0.0.1", 3001, context))
            using (var publisher = Publisher.Create("127.0.0.1", 3002, 3003, context))
            {
                worker.AddTaskHandler(new TaskHandlerDescriptor(typeof(SimpleTestHandler), "namespace:"));
                worker.Start();
                publisher.Start();

                Console.ReadLine();
            }
        }
    }
}
