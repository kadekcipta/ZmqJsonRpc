using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MSA.Zmq.JsonRpc;
using MSA.Zmq.JsonRpc.Models;

namespace MSA.Subscriber.Tester
{
    static class Program
    {
        public static Client _client;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            _client = Client.Connect("localhost", 3001, ClientMode.Rpc);
            _client.ServiceError += new ErrorEventHandler(_client_ServiceError);
            var resp = _client.CallMethod<IList<TaskDescriptor>>("systems:GetAvailableTasks");
            if (resp != null)
            {
                foreach (var td in resp)
                {
                    System.Diagnostics.Debug.WriteLine(td.Name);
                }
            }

            //MessageBox.Show(resp);

            //_client.CallMethodAsync<string>("EchoString", (s) =>
            //{
            //    Console.WriteLine(s);
            //}, "Hello ZMQ ");

            //for (int i = 0; i < 20; i++)
            //{
            //    _client.CallMethod<string>("EchoString", (s) =>
            //    {
            //        Console.WriteLine(s);
            //    }, "Hello ZMQ " + i.ToString());
            //}

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

            //Console.ReadLine();
            _client.Dispose();
        }

        static void _client_ServiceError(object sender, JsonRpcException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
