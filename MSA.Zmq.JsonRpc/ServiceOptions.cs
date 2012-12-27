using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public enum ServiceType { Router, Worker }
    public enum ServiceAction { None, Install, Uninstall, Run, Help }

    public sealed class ServiceOptions
    {
        public ServiceOptions()
        {
            Router = "*";
            Mode = ServiceType.Router;
            RunConsole = true;
            WindowsServiceAction = ServiceAction.None;
        }

        public ServiceType Mode { get; set; }

        /// <summary>
        /// If workers are running in the same box with router
        /// </summary>
        public uint[] Ports { get; set; }

        /// <summary>
        /// If router is running in the different box with service, router address will be used
        /// </summary>
        public string Router { get; set; }

        public bool RunConsole { get; set; }
        public ServiceAction WindowsServiceAction { get; set; }
        public string ServiceName { get; set; }

        public bool IsValid
        {
            get
            {
                if (Ports != null)
                {
                    if (Mode == ServiceType.Router)
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
