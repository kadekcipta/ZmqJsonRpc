using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSA.Zmq.JsonRpc
{
    public class JsonRpcException : Exception
    {
        public JsonRpcException(int code, string message)
            : base(message)
        {
            Code = code;
        }

        public int Code { get; private set; }
    }

    public class ParseErrorExeption : JsonRpcException
    {
        public ParseErrorExeption(string message) : base(-32700, message) { }
    }

    public class InvalidRequestExeption : JsonRpcException
    {
        public InvalidRequestExeption(string message) : base(-32600, message) { }
    }

    public class MethodNotFoundException : JsonRpcException
    {
        public MethodNotFoundException(string message) : base(-32601, message) { }
    }

    public class InvalidParametersException : JsonRpcException
    {
        public InvalidParametersException(string message) : base(-32602, message) { }
    }

    public class InternalErrorException : JsonRpcException
    {
        public InternalErrorException(string message) : base(-32603, message) { }
    }

    public class ServerErrorException : JsonRpcException
    {
        public ServerErrorException(string message) : base(-32000, message) { }
    }

    public class UnauthorizedException : JsonRpcException
    {
        public UnauthorizedException(string message) : base(-32000, message) { }
    }
}
