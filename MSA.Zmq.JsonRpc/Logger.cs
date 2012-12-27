using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Config;
using System.Reflection;

namespace MSA.Zmq.JsonRpc
{
    public sealed class Logger
    {
        private static ILog _logger;

        static Logger()
        {
            XmlConfigurator.Configure();
            _logger = LogManager.GetLogger(Assembly.GetExecutingAssembly().GetType());
        }

        public static ILog Instance
        {
            get
            {
                return _logger;
            }
        }
    }
}
