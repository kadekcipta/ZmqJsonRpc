using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using MSA.Zmq.JsonRpc;
using MSA.LocalCache.Models;
using MSA.Repository;
using MSA.Repository.ServiceHandler;

namespace MSA.Publisher.Tester
{
    [JsonRpcServiceHandler]
    class PatientLookup
    {
        [JsonRpcMethod(Authorize=true, LogCall=false, Description="Just say hello", RequiredRoles="admin, developer")]
        public string SayHello()
        {
            return "Hello there";
        }

        [JsonRpcMethod]
        public double AddNumber(double v1, double v2)
        {
            return v1 + v2;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (var worker = Worker.Create("192.168.2.2", 3001))
            {
                worker.AddTaskHandler(new TaskHandlerDescriptor(typeof(CommonLookup), null));
//                worker.AddTaskHandler(new TaskHandlerDescriptor(typeof(PatientLookup), "patients:"));
                worker.Start();

                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
            }
        }
    }
}
