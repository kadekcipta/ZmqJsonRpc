using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using ZeroMQ;
using Newtonsoft.Json;

namespace MSA.Zmq.JsonRpc
{
    public class Publisher : JsonRpcZmqServiceBase
    {
        private uint _pubPort;
        private uint _pullPort;
        private bool _working;
        private string _address;
        private string _stopCommandToken;

        public event StartedEventHandler Started;
        public event StoppedEventHandler Stopped;

        public static Publisher Create(string address, uint pubPort, uint pullPort, ZmqContext context = null)
        {
            return new Publisher(address, pubPort, pullPort, context);
        }

        private Publisher(string address, uint pubPort, uint pullPort, ZmqContext context = null)
            : base(context)
        {
            _address = address;
            _pubPort = pubPort;
            _pullPort = pullPort;
            _working = false;
            // Used for termination token than tend to be unique to prevent accidental/abuse termination from client request
            _stopCommandToken = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Dispatcher for started event
        /// </summary>
        private void OnStarted()
        {
            if (Started != null)
            {
                Started(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Dispatcher for stopped event
        /// </summary>
        private void OnStopped()
        {
            if (Stopped != null)
            {
                Stopped(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Loop threads for publisher and replyer
        /// </summary>
        public override void Start()
        {
            if (!_working)
            {
                ThreadPool.QueueUserWorkItem(obj => StartHandleRequest());
                _working = true;
            }
        }

        private void StartHandleRequest()
        {
            using (ZmqSocket pubSocket = ServiceContext.CreateSocket(SocketType.REP))
            using (ZmqSocket pullSocket = ServiceContext.CreateSocket(SocketType.PULL))
            {
                pubSocket.Bind(String.Format("tcp://{0}:{1}", _address, _pubPort));
                pubSocket.Linger = TimeSpan.FromMilliseconds(0);
                
                pullSocket.Bind(String.Format("tcp://{0}:{1}", _address, _pullPort));
                pullSocket.Linger = TimeSpan.FromMilliseconds(0);
                
                try
                {
                    _working = true;

                    while (_working)
                    {
                        var message = pullSocket.Receive(Encoding.UTF8);
                        _working = !_stopCommandToken.Equals(message, StringComparison.OrdinalIgnoreCase);
                        if (!_working)
                        {
                            break;
                        }
                        else if (!String.IsNullOrEmpty(message))
                        {
                            pubSocket.Send(message, Encoding.UTF8);
                        }
                    }
                }
                catch (ZeroMQ.ZmqException ex)
                {
                    Logger.Instance.Debug(ex);
                }
                catch (System.Exception ex)
                {
                    Logger.Instance.Error(ex);
                }

                OnTerminated();
            }
        }

        public override void Stop()
        {
            using (var socket = ServiceContext.CreateSocket(SocketType.PUSH))
            {
                socket.Connect(String.Format("tcp://{0}:{1}", _address, _pullPort));
                socket.Linger = TimeSpan.FromMilliseconds(0);
                socket.Send(_stopCommandToken, Encoding.UTF8);
            }
        }
    }
}

