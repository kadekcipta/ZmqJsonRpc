using System;
using MSA.Zmq.JsonRpc;
using System.Configuration.Install;
using System.Collections;
using System.Linq;
using System.ServiceProcess;
using System.Configuration;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace MSA.Zmq.Service
{
    class Program
    {
        static string _currentConfigPath;

        static void PrintMessage(string message, bool isStrong)
        {
            var savedColor = Console.ForegroundColor;
            if (isStrong)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine(message);
            Console.ForegroundColor = savedColor;
        }

        static bool IsRequiredFileType(string fileExt)
        {
            return fileExt.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                   fileExt.Equals(".config", StringComparison.OrdinalIgnoreCase);
        }

        static void WatchServiceAssemblies(string directory, Action<string> changedFileNotification)
        {
            var fsw = new FileSystemWatcher(directory, "*.*");
            fsw.Changed += (object sender, FileSystemEventArgs e) =>
            {
                if (IsRequiredFileType(Path.GetExtension(e.Name)))
                {
                    try
                    {
                        fsw.EnableRaisingEvents = false;
                        if (changedFileNotification != null)
                        {
                            changedFileNotification(e.FullPath);
                        }
                    }
                    finally
                    {
                        fsw.EnableRaisingEvents = true;
                    }
                }
            };

            fsw.EnableRaisingEvents = true;
        }

        static string GetStartupDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        static TaskHandlerDescriptor[] LoadTaskHandlerDescriptors()
        {
            var taskDescriptors = new List<TaskHandlerDescriptor>();
            var cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var section = cfg.Sections["zmsa-handlers"] as ZMSAConfigurationSectionHandler;
            if (section != null && section.Handlers != null)
            {
                foreach (var handler in section.Handlers)
                {
                    var handlerElement = handler as ZMSAServiceHandlerElement;
                    if (handlerElement != null)
                    {
                        try
                        {
                            var assemblyPath = Path.Combine(GetStartupDirectory(), handlerElement.AssemblyName + ".dll");
                            var asm = Assembly.Load(File.ReadAllBytes(assemblyPath));

                            foreach (var t in asm.GetTypes())
                            {
                                var attributes = t.GetCustomAttributes(typeof(JsonRpcServiceHandlerAttribute), false);
                                if (attributes != null && attributes.Length > 0)
                                {
                                    taskDescriptors.Add(new TaskHandlerDescriptor(t, handlerElement.EndPointPrefix));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Error(ex);
                        }                        
                    }
                }
            }

            return taskDescriptors.ToArray();
        }

        static void Main(string[] args)
        {
            var cancelled = false;
            var config = args.Where((s) => s.Contains("--config-path=")).SingleOrDefault();
            if (!String.IsNullOrEmpty(config))
            {
                var configFileName = config.Split('=')[1];
                if (!String.IsNullOrEmpty(configFileName))
                {
                    _currentConfigPath = Path.Combine(Directory.GetCurrentDirectory(), configFileName);
                    if (!File.Exists(_currentConfigPath))
                        throw new FileNotFoundException(String.Format("File {0} is not found in current directory", configFileName));
                }
            }

            ServiceRunner runner = new ServiceRunner(args, LoadTaskHandlerDescriptors());

            if (!runner.Options.IsValid)
            {
                Console.WriteLine(String.Format(runner.GetHelp(), System.IO.Path.GetFileName(Environment.GetCommandLineArgs()[0])));
                return;
            }

            if (runner.Options.RunConsole) // console mode
            {
                Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => {
                    e.Cancel = true;
                    cancelled = true;
                    Logger.Instance.Info("Exiting...");
                    runner.Dispose();
                    return;
                };

                runner.Start();
                WatchServiceAssemblies(Directory.GetCurrentDirectory(), (fname) => {
                    // Reload the task handlers, not ready yet
                    if (runner.Options.Mode != MSA.Zmq.JsonRpc.ServiceMode.Router)
                    {
                        if (Environment.UserInteractive)
                        {
                            PrintMessage("Reloading task handlers...", true);
                        }

                        runner.Reload(LoadTaskHandlerDescriptors());
                    }
                });

                if (runner.Options.Mode != ServiceMode.None)
                    while (!cancelled) { }
            }
            else
            {
                var serviceAction = runner.Options.ServiceAction;
                if (serviceAction == ServiceAction.Install)
                {
                    // install the service with arguments
                    AssemblyInstaller installer = new AssemblyInstaller(typeof(Program).Assembly, args);
                    IDictionary state = new Hashtable();

                    try
                    {
                        installer.UseNewContext = true;
                        if (runner.Options.Mode == ServiceMode.Router || runner.Options.Mode == ServiceMode.MultiWorker || 
                            runner.Options.Mode == ServiceMode.Worker || runner.Options.Mode == ServiceMode.Publisher)
                        {
                            // remove the --install-service arg
                            var newArgs = args.Where(s => !s.StartsWith("--install-service")).ToArray();
                            installer.Installers.Add(new ZMQServiceInstaller(runner.Options.ServiceName, newArgs));
                        }

                        if (serviceAction == ServiceAction.Install)
                        {
                            installer.Install(state);
                            installer.Commit(state);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex);
                        installer.Rollback(state);
                        throw;
                    }
                    finally
                    {
                        installer.Dispose();
                    }
                }
                else if (serviceAction == ServiceAction.Help)
                {
                    Console.WriteLine(String.Format(runner.GetHelp(), System.IO.Path.GetFileName(Environment.GetCommandLineArgs()[0])));
                }
                else // run the service
                {
                    ServiceBase.Run(new ZMQService(runner));
                }                
            }
        }
    }
}
