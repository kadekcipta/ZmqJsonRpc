using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public class TaskHandlerDescriptor
    {
        public TaskHandlerDescriptor(Type handlerType, string endpointPrefix)
        {
            HandlerType = handlerType;
            EndPointPrefix = endpointPrefix;
        }

        public Type HandlerType { get; set; }
        public string EndPointPrefix { get; set; }
    }
}
