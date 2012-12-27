using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSA.Zmq.JsonRpc;
using ZeroMQ;
using System.Reflection;
using System.IO;

namespace MSA.Zmq.JsonRpc
{
    public sealed class ServiceRunner : IDisposable
    {
        private ZmqContext _context;
        private bool _disposed;
        private IList<JsonRpcZmqServiceBase> _services;
        private IList<TaskHandlerDescriptor> _handlerDescriptors;

        public ServiceRunner(string[] args, params TaskHandlerDescriptor[] handlerDescriptors)
        {
            _disposed = false;
            _services = new List<JsonRpcZmqServiceBase>();
            _context = ZmqContext.Create();

            if (handlerDescriptors != null)
            {
                _handlerDescriptors = handlerDescriptors.ToList();
            }

            Options = new ServiceOptions();
            ParseArguments(args);
        }

        public ServiceOptions Options { get; set; }

        private void InitializeServices()
        {
            DestroyServices();

            if (Options.IsValid)
            {
                if (Options.Mode == ServiceType.Router)
                {
                    var router = Router.Create(Options.Ports[1], Options.Ports[0], _context);
                    _services.Add(router);
                }
                else
                {
                    // Check wether run as worker group or single worker by differentiating with router value
                    if (String.IsNullOrEmpty(Options.Router))
                    {
                        // Single worker
                        var worker = Worker.Create(Options.Ports[0], _context);
                        foreach (var ht in _handlerDescriptors)
                        {
                            worker.AddTaskHandler(ht);
                        }

                        _services.Add(worker);
                    }
                    else
                    {
                        // Worker group
                        var workerId = 1;
                        foreach (var port in Options.Ports)
                        {
                            var worker = Worker.Create(port, workerId.ToString(), _context);
                            worker.RouterAddress = Options.Router;
                            foreach (var ht in _handlerDescriptors)
                            {
                                worker.AddTaskHandler(ht);
                            }

                            _services.Add(worker);
                            workerId++;
                        }
                    }
                }
            }
            else
            {
                Print(String.Format(GetHelp(), System.IO.Path.GetFileName(Environment.GetCommandLineArgs()[0])));
            }
        }

        private void Print(string message)
        {
            Console.WriteLine(message);
        }

        private void Info(string message)
        {
            Logger.Instance.Info(message);
        }

        private void DestroyServices()
        {
            foreach (var service in _services)
            {
                service.Dispose();
            }
        }

        public void Reload(TaskHandlerDescriptor[] handlerDescriptors)
        {
            foreach (var service in _services)
            {
                var worker = service as Worker;
                if (worker != null)
                {
                    try
                    {
                        worker.BeginSuspend();
                        worker.RemoveAllResolvers();
                        foreach (var ht in handlerDescriptors)
                        {
                            worker.AddTaskHandler(ht);
                        }
                    }
                    finally
                    {
                        worker.EndSuspend();
                    }
                }
            }
        }

        public void Start()
        {
            try
            {
                InitializeServices();

                foreach (var service in _services)
                {
                    service.Start();
                }
            }
            catch (ZeroMQ.ZmqException ex)
            {
                Logger.Instance.Error(ex);
            }
            catch (System.Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        public void Stop()
        {
            try
            {
                DestroyServices();
            }
            catch (ZeroMQ.ZmqException ex)
            {
                Logger.Instance.Error(ex);
            }
            catch (System.Exception ex)
            {
                Logger.Instance.Error(ex);
            }
        }

        private void HandleOption(string key, string value)
        {
            switch (key)
            {
                case "HELP":
                    Options.WindowsServiceAction = ServiceAction.Help;
                    break;
                case "ROUTER":
                    // --router=<backend-port>:<frontend-port>
                    Options.Mode = ServiceType.Router;
                    Options.Ports = value.Split(':').Select(s => Convert.ToUInt32(s.Trim())).ToArray();
                    Options.Router = "*"; // local box, not used here since the router will automatically bind to local default interface
                    break;

                case "WORKER-GROUP":
                    Options.Mode = ServiceType.Worker;
                    // --worker-group=<router-url>#<port-list>
                    var parts = value.Split('#');
                    Options.Router = parts[0].Trim();
                    Options.Ports = parts[1].Split(',').Select(s => Convert.ToUInt32(s.Trim())).ToArray();
                    break;

                case "WORKER":
                    // --worker=<port>
                    Options.Mode = ServiceType.Worker;
                    Options.Router = String.Empty;
                    Options.Ports = new uint[] { Convert.ToUInt32(value) };
                    break;

                case "INSTALL":
                    Options.WindowsServiceAction = ServiceAction.Install;
                    break;

                case "RUNSERVICE":
                    Options.WindowsServiceAction = ServiceAction.Run;
                    break;

                case "UNINSTALL":
                    Options.WindowsServiceAction = ServiceAction.Uninstall;
                    break;
            }

            Options.RunConsole = Options.WindowsServiceAction == ServiceAction.None;
        }

        public string GetHelp()
        {
            using (var resStream = new StreamReader(this.GetType().Assembly.GetManifestResourceStream("MSA.Zmq.JsonRpc.README.txt")))
            {
                return resStream.ReadToEnd();
            }
        }

        /// <summary>
        /// Parse arguments: --argname=argvalues
        /// </summary>
        /// <param name="args"></param>
        private void ParseArguments(string[] args)
        {
            foreach (var arg in args)
            {
                var _arg = arg.Trim('-');
                var parts = _arg.Split('=');
                if (parts != null)
                {
                    if (parts.Length == 2)
                        HandleOption(parts[0].ToUpper(), parts[1]);
                    else if (parts.Length == 1)
                        HandleOption(parts[0].ToUpper(), String.Empty);
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();

                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }

                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        #endregion
    }
}
