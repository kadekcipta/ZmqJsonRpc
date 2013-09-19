using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSA.Zmq.JsonRpc;
using System.Diagnostics;

namespace Client.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var client = MSA.Zmq.JsonRpc.Client.CreateJsonRpcContext("tcp://127.0.0.1:3001"))
            using (var subscriber = MSA.Zmq.JsonRpc.Client.CreateSubscriberContext("tcp://127.0.0.1:3002"))
            using (var pusher = MSA.Zmq.JsonRpc.Client.CreatePushContext("tcp://127.0.0.1:3003"))
            {
                // subscribe for event triggered from here
                subscriber.Subscribe("AddNumberEvent", (s) =>
                {
                    Console.WriteLine(s);
                });

                // subscribe for event triggered from another agent
                subscriber.Subscribe("oob:notification", (s) =>
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("OOB Notification: {0}", s);
                    Console.ResetColor();
                });

                // add some delay
                System.Threading.Thread.Sleep(1000);

                for (var i = 0; i < 10; i++)
                {
                    Console.WriteLine("Calling remote method");
                    client.CallMethodAsync<double>("namespace:AddNumber", (ret) =>
                    {
                        // just for fun to publish the event
                        pusher.Push("AddNumberEvent", String.Format("AddNumber called successfully => {0}", ret));
                        //Console.WriteLine(ret);
                    }, 200, 50*i);

                    System.Threading.Thread.Sleep(100);
                }

                Console.ReadLine();
            }
        }
    }
}
