using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public enum ServiceMode { None, Router, Worker, MultiWorker, Publisher }
    public enum ServiceAction { None, Install, Run, Help }

    public sealed class ServiceOptions
    {
        public ServiceOptions()
        {
            Router = "*";
            Mode = ServiceMode.None;
            RunConsole = true;
            ServiceAction = ServiceAction.None;
        }

        public ServiceMode Mode { get; set; }
        public uint[] Ports { get; set; }
        public string Router { get; set; }
        public bool RunConsole { get; set; }
        public ServiceAction ServiceAction { get; set; }
        public string ServiceName { get; set; }
        public string HostName { get; set; }

        public bool IsValid
        {
            get
            {
                if (String.IsNullOrEmpty(HostName))
                    return false;

                if (ServiceAction == JsonRpc.ServiceAction.Install)
                {
                    return !String.IsNullOrEmpty(ServiceName);
                }

                if (Ports != null)
                {
                    if (Mode == ServiceMode.Router)
                        if (Ports.Length != 2 || Router != "*")
                            return false;

                    return Ports != null; // Router is null when configured as single worker
                }
                else
                    return false;
            }
        }

        public override string ToString()
        {
            var ports = "";
            if (Ports != null)
            {
                foreach (var p in Ports)
                {
                    ports += p.ToString() + " ";
                }
            }

            return String.Format("RouterHost:{0}, ServicePorts:{1}, ServiceName:{2}", Router, ports, ServiceName);
        }
    }
}
