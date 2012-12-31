using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using System.Reflection;

namespace MSA.Zmq.JsonRpc
{
    public delegate void TerminatedEventHandler(object sender, EventArgs e);

    public class JsonRpcZmqServiceBase: IDisposable
    {
        private bool _disposed;
        private ZmqContext _context;
        private bool _ownedContext;

        public event TerminatedEventHandler Terminated;

        public JsonRpcZmqServiceBase(ZmqContext context = null)
        {
            _ownedContext = true;
            _disposed = false;

            if (context != null)
            {
                _context = context;
                _ownedContext = false;
            }

            Active = false;
        }

        ~JsonRpcZmqServiceBase()
        {
            Dispose(false);
        }

        public string RouterAddress { get; set; }
        public bool Active { get; protected set; }

        protected virtual void OnTerminated()
        {
            if (Terminated != null)
            {
                Terminated(this, EventArgs.Empty);
            }
        }

        protected ZmqContext ServiceContext 
        {
            get 
            {
                if (_ownedContext && _context == null)
                {
                    _context = ZmqContext.Create();
                }

                return _context; 
            }
        }

        protected virtual void DestroyContext()
        {
            if (_ownedContext && _context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
        }

        protected void Info(string message)
        {
            Logger.Instance.Info(message);
        }

        protected void LogError(System.Exception ex)
        {
            Logger.Instance.Error(ex);
        }

        protected void Debug(string message)
        {
            Logger.Instance.Debug(message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Stop();
                DestroyContext();

                if (disposing)
                {
                }

                _disposed = true;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
