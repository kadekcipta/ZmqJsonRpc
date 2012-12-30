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
            using (var client = MSA.Zmq.JsonRpc.Client.Connect("127.0.0.1", 3001, ClientMode.Rpc))
            {
                //var n = 100;
                //var counter = 0;
                //var sw = Stopwatch.StartNew();
                //for (var x = 0; x < n; x++)
                //{
                //    //var result = client.CallMethod<double>("AddNumber", x, x*2);
                //    client.CallMethodAsync<double>("AddNumber", (ret) =>
                //    {
                //        //Console.WriteLine("Calling AddNumber: {0}", ret);
                //        if (counter >= n - 1)
                //        {
                //            sw.Stop();
                //            var d = (n * 1000.0) / sw.ElapsedMilliseconds;
                //            Console.WriteLine("Elapsed: {0} ms = {1} in seconds", sw.ElapsedMilliseconds, Math.Round(d));
                //        }

                //        counter++;
                //    }, x, x * 2);
                //}


                //client.CallMethodAsync("Dumb", "This is a void returned method ");
                //client.CallMethodAsync<string>("Echo", (ret) =>
                //{
                //    Console.WriteLine("Calling Echo: {0}", ret);
                //}, "Hello ZMQ JSON-RPC");

                //client.CallMethodAsync<DateTime>("EchoDate", (ret) =>
                //{
                //    Console.WriteLine(ret);
                //}, DateTime.Now);

                //var ed = client.CallMethod<DateTime>("EchoDate", DateTime.Now);
                //Console.WriteLine(ed);

                //client.CallMethodAsync<IList<MSA.Zmq.JsonRpc.Models.TaskDescriptor>>("systems:GetAvailableTasks", (list) =>
                //{
                //    foreach (var task in list)
                //    {
                //        Console.WriteLine(task.Name);
                //    }
                //});

                var cmd = "";
                do
                {
                    Console.Write("\nYour command: ");
                    cmd = Console.ReadLine();
                    if (!String.IsNullOrEmpty(cmd))
                    {
                        client.CallMethodAsync<string>("systems:Cmd", (o) =>
                        {
                            Console.WriteLine(o);
                            Console.Write("\nYour command: ");
                        }, cmd);
                    }
                }
                while (cmd != "exit");

                //Console.ReadLine();
            }
        }
    }
}
