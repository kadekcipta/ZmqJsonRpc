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
    public enum ServerMode
    {
        Worker,
        MultiWorker,
        Router,
        Publisher
    }

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
            if (_context == null)
                _context = ZmqContext.Create();

            switch (Options.Mode)
            {
                case ServiceMode.Worker:
                case ServiceMode.MultiWorker:
                    // create single worker with binding port on first ports list
                    if (Options.Mode == ServiceMode.Worker)
                    {
                        var worker = Worker.Create(Options.HostName, Options.Ports[0], _context);
                        foreach (var ht in _handlerDescriptors)
                            worker.AddTaskHandler(ht);
                        _services.Add(worker);
                    }
                    else
                    {
                        // create multi worker with binding port on ports list
                        var workerId = 1;
                        foreach (var port in Options.Ports)
                        {
                            var worker = Worker.Create(Options.HostName, port, workerId.ToString(), _context);
                            worker.RouterAddress = Options.Router;
                            foreach (var ht in _handlerDescriptors)
                            {
                                worker.AddTaskHandler(ht);
                            }
                            _services.Add(worker);
                            workerId++;
                        }
                    }
                    break;
                case ServiceMode.Publisher:
                    var publisher = Publisher.Create(Options.HostName, Options.Ports[0], Options.Ports[1], _context);
                    _services.Add(publisher);
                    break;
                case ServiceMode.Router:
                    // TODO: Router implementation
                    break;
            }
        }

        private void Print(string message)
        {
            if (Environment.UserInteractive)
                Console.WriteLine(message);
        }

        private void Info(string message)
        {
            Logger.Instance.Info(message);
        }

        private void DestroyServices()
        {
            try
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
            }
            catch
            {
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

        /// <summary>
        /// Format: --mode=<MODE>:<HOST>:<COMMA-SEPARATED-PORT-LIST>:[<ROUTER-URL>]
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void HandleOption(string key, string value)
        {
            if (key.Equals("MODE", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrEmpty(value))
            {
                var values = value.Split(':').Where(s => !String.IsNullOrEmpty(s)).Select(s => s.Trim()).ToList();
                if (values.Count >= 3)
                {
                    var modeValue = values[0];
                    var host = values[1];
                    var ports = values[2].Split(',').Where(s => !String.IsNullOrEmpty(s)).Select<string, uint>(s => Convert.ToUInt32(s)).ToArray();
                    var routerUrl = String.Empty;
                    if (values.Count >= 4)
                        routerUrl = values[3];

                    Options.HostName = host.Trim();
                    Options.Ports = ports;
                    Options.Router = routerUrl;

                    switch (modeValue)
                    {
                        case "ROUTER":
                            Options.Mode = ServiceMode.Router;
                            break;
                        case "WORKER":
                            Options.Mode = ServiceMode.Worker;
                            break;
                        case "MULTI-WORKER":
                            Options.Mode = ServiceMode.MultiWorker;
                            break;
                        case "PUBLISHER":
                            Options.Mode = ServiceMode.Publisher;
                            break;
                    }
                }
            }
            else if (key.Equals("INSTALL-SERVICE", StringComparison.OrdinalIgnoreCase))
            {
                Options.ServiceAction = ServiceAction.Install;
                Options.ServiceName = value.Trim().Replace(" ", "");
            }
            else if (key.Equals("RUN-SERVICE", StringComparison.OrdinalIgnoreCase))
            {
                Options.ServiceAction = ServiceAction.Run;
            }
            else if (key.Equals("HELP", StringComparison.OrdinalIgnoreCase))
            {
                Options.ServiceAction = ServiceAction.Help;
            }

            Options.RunConsole = Options.ServiceAction == ServiceAction.None;
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
