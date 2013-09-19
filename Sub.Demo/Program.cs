using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSA.Zmq.JsonRpc;

namespace Sub.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Subscribe for event");
            using (var client = Client.CreateSubscriberContext("tcp://127.0.0.1:3002"))
            {
                client.Subscribe("AddNumberEvent", (s) =>
                {
                    Console.WriteLine(s);
                });

                Console.ReadLine();
            }
        }
    }
}
