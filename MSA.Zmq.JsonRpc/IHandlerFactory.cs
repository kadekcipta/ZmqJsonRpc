using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public interface IHandlerFactory
    {
        Object CreateHandler(Type type);
    }
}
