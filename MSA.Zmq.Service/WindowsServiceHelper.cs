using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSA.Zmq.JsonRpc;
using System.Configuration.Install;
using System.Collections;
using System.ServiceProcess;
using System.ComponentModel;
using System.Configuration;
using Microsoft.Win32;
using System.Reflection;

namespace MSA.Zmq.Service
{
    internal class ZMQService : ServiceBase
    {
        private ServiceRunner _runner;
        public ZMQService(ServiceRunner runner)
        {
            _runner = runner;
        }

        protected override void OnStart(string[] args)
        {
            _runner.Start();
            Logger.Instance.Info("Router Started");
        }

        protected override void OnStop()
        {
            _runner.Stop();
            Logger.Instance.Info("Service uninstalled successfully");
        }
    }


    internal class ZMQServiceInstaller : Installer
    {
        private string ServiceName { get; set; }
        private string Arguments { get; set; }

        public ZMQServiceInstaller(string serviceName, string[] args)
        {
            ServiceName = serviceName;
            var process = new ServiceProcessInstaller();
            process.Account = ServiceAccount.LocalSystem;
            var service = new ServiceInstaller();
            service.Description = "Magicsoft-Asia System Service Installer";
            service.DisplayName = "ZMQ JSON-RPC " + serviceName;
            service.ServiceName = serviceName;
            service.StartType = ServiceStartMode.Manual;
            Installers.Add(process);
            Installers.Add(service);

            var arguments = new StringBuilder();
            foreach (var arg in args)
            {
                if (arg.Equals("--install", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--help", StringComparison.OrdinalIgnoreCase))
                    continue;

                arguments.Append(arg);
                arguments.Append(" ");
            }
            arguments.Append("--runservice");

            Arguments = arguments.ToString();

            Logger.Instance.Info("Arguments: " + Arguments);
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            var imagePath = Assembly.GetExecutingAssembly().Location;
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + ServiceName, true))
            {
                key.SetValue("ImagePath", imagePath + " " + Arguments, RegistryValueKind.ExpandString);
                key.Close();
            }

            base.OnAfterInstall(savedState);
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            var imagePath = Assembly.GetExecutingAssembly().Location;
            Registry.LocalMachine.DeleteSubKey(@"SYSTEM\CurrentControlSet\Services\" + ServiceName, false);

            base.OnAfterUninstall(savedState);
        }
    }
}