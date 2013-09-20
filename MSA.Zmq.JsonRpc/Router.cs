using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZeroMQ;
using System.Threading;

namespace MSA.Zmq.JsonRpc
{
    /// <summary>
    /// Router is a device that will accept all client requests and forwarded to services
    /// and in turns forward the replies from services back to clients
    /// 
    /// Pattern:
    /// ROUTER - DEALER + control channel
    /// </summary>
    public class Router: JsonRpcZmqBase
    {
        private ZmqSocket _frontendSocket;
        private ZmqSocket _backendSocket;
        private string _stopCommandToken;
        private uint _frontendPort;
        private uint _backendPort;
        private string _backendAddress;
        private string _frontendAddress;
        private bool _routing;
        private Thread _routingThread;

        /// <summary>
        /// Currently supports only single address *, no multihomed interface yet
        /// We will wait for other situation when it's needed
        /// </summary>
        /// <param name="frontendAddress"></param>
        /// <param name="frontendPort"></param>
        /// <param name="backendAddress"></param>
        /// <param name="backendPort"></param>
        /// <returns></returns>
        public static Router Create(uint frontendPort, uint backendPort, ZmqContext context)
        {
            return new Router(frontendPort, backendPort, context);
        }
             
        public Router(uint frontendPort, uint backendPort, ZmqContext context): base(context)
        {
            _stopCommandToken = Guid.NewGuid().ToString();
            _routing = false;
            // stick on there address first until need for multihomed/bridging the different network
            _backendAddress = "*";
            _frontendAddress = "*";
            _frontendPort = frontendPort;
            _backendPort = backendPort;
        }

        /// <summary>
        ///         +------------+   _____
        /// W <-->  |   ROUTER   |  /
        /// W <--> o|B          F|o <-----
        /// W <-->  |   DEALER   |  \_____
        ///         +------------+
        /// </summary>
        private void PrepareSockets()
        {
            // Create frontend/router socket
            _frontendSocket = ServiceContext.CreateSocket(SocketType.ROUTER);
            _frontendSocket.ReceiveReady += new EventHandler<SocketEventArgs>(_frontendSocket_ReceiveReady);
            _frontendSocket.Linger = TimeSpan.FromMilliseconds(0);
            _frontendSocket.Bind(String.Format("tcp://{0}:{1}", _frontendAddress, _frontendPort));
            _frontendSocket.SendReady += new EventHandler<SocketEventArgs>(_frontendSocket_SendReady);

            // Create backend/dealer socket
            _backendSocket = ServiceContext.CreateSocket(SocketType.DEALER);
            _backendSocket.ReceiveReady += new EventHandler<SocketEventArgs>(_backendSocket_ReceiveReady);
            _backendSocket.Linger = TimeSpan.FromMilliseconds(0);
            _backendSocket.Bind(String.Format("tcp://{0}:{1}", _backendAddress, _backendPort));
            _backendSocket.SendReady += new EventHandler<SocketEventArgs>(_backendSocket_SendReady);
        }

        void _frontendSocket_SendReady(object sender, SocketEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Frontend send ready {0}", e.Socket.LastEndpoint);
            //RelayMessage(e.Socket, _backendSocket);
        }

        void _backendSocket_SendReady(object sender, SocketEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Backend send ready {0}", e.Socket.LastEndpoint);
            //RelayMessage(e.Socket, _frontendSocket);
        }

        private void RelayMessage(ZmqSocket source, ZmqSocket destination)
        {
            try
            {
                var buffer = new byte[4096];
                bool isProcessing = true;
                while (_routing && isProcessing)
                {
                    
                    //int size = 0;
                    source.Receive(buffer);
                    if (IsStopCommand(Encoding.UTF8.GetString(buffer)))
                    {
                        _routing = false;
                        break;
                    }
                    else
                    {
                        if (source.ReceiveMore)
                            destination.SendMore(buffer);
                            //destination.SendMore(message, Encoding.UTF8);
                        else
                            destination.Send(buffer);
                            //destination.Send(message, Encoding.UTF8);

                        isProcessing = source.ReceiveMore;
                    }
                }
            }
            catch (ZeroMQ.ZmqException ex)
            {
                Logger.Instance.Debug(ex);
            }
        }

        void _backendSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Frontend recv ready");
            RelayMessage(e.Socket, _frontendSocket);
        }

        void _frontendSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Frontend recv ready");
            RelayMessage(e.Socket, _backendSocket);
        }

        private void StartRouting()
        {
            if (_routing) return;
            
            Info("ROUTER: Started");
            Info(String.Format("ROUTER: Frontend Port: {0} Backend Port: {1}", _frontendPort, _backendPort ));

            _routingThread = new Thread(() => 
            {
                PrepareSockets();
                //PollItem[] pollItems = new PollItem[2];
                //pollItems[0] = _frontendSocket.CreatePollItem(IOMultiPlex.POLLIN);
                //pollItems[0].PollInHandler += new PollHandler(FrontendPollInHandler);

                //pollItems[1] = _backendSocket.CreatePollItem(IOMultiPlex.POLLIN);
                //pollItems[1].PollInHandler += new PollHandler(BackendPollInHandler);
                var p = new Poller();
                p.AddSocket(_frontendSocket);
                p.AddSocket(_backendSocket);

                //p.AddSockets(new [] { _frontendSocket, _backendSocket });
                _routing = true;
                while (_routing)
                {
                    try
                    {
                        var ret = p.Poll();
                        if (ret == ZeroMQ.ErrorCode.ETERM || ret == ZeroMQ.ErrorCode.EFAULT || ret == ZeroMQ.ErrorCode.EINTR)
                        {
                            _routing = false;
                        }
                    }
                    catch (ZeroMQ.ZmqException)
                    {
                        _routing = false;
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Instance.Error(ex);
                        _routing = false;
                    }
                }

                _frontendSocket.Dispose();
                _backendSocket.Dispose();

                _frontendSocket = null;
                _backendSocket = null;

                _routingThread = null;
                _routing = false;

                Logger.Instance.Info("ROUTER: Terminated gracefully");
            });

            if (_routingThread != null)
            {
                _routingThread.IsBackground = true;
                _routingThread.Start();
            }
        }

        private bool IsStopCommand(string data)
        {
            return data.Equals(_stopCommandToken, StringComparison.OrdinalIgnoreCase);
        }

        //private void FrontendPollInHandler(ZmqSocket socket, IOMultiPlex revents)
        //{

        //    //  Process all parts of the message and send to backend worker
        //    try
        //    {
        //        bool isProcessing = true;
        //        while (_routing && isProcessing)
        //        {
        //            byte[] message = socket.Recv();
        //            if (IsStopCommand(message))
        //            {
        //                _routing = false;
        //                break;
        //            }
        //            else
        //            {
        //                _backendSocket.Send(message, socket.RcvMore ? SendRecvOpt.SNDMORE : 0);
        //                isProcessing = socket.RcvMore;
        //            }
        //        }
        //    }
        //    catch (ZMQ.Exception ex)
        //    {
        //        Logger.Instance.Debug(ex);
        //    }
        //}

        //private void BackendPollInHandler(ZmqSocket socket, IOMultiPlex revents)
        //{
        //    //  Send messages from backend worker to client
        //    try
        //    {
        //        bool isProcessing = true;
        //        while (_routing && isProcessing)
        //        {
        //            byte[] message = socket.Recv();
        //            if (IsStopCommand(message))
        //            {
        //                _routing = false;
        //                break;
        //            }
        //            else
        //            {
        //                _frontendSocket.Send(message, socket.RcvMore ? SendRecvOpt.SNDMORE : 0);
        //                isProcessing = socket.RcvMore;
        //            }
        //        }
        //    }
        //    catch (ZMQ.Exception ex)
        //    {
        //        Logger.Instance.Debug(ex);
        //    }
        //}
        //private void FrontendPollInHandler(ZmqSocket socket, IOMultiPlex revents)
        //{

        //    //  Process all parts of the message and send to backend worker
        //    try
        //    {
        //        bool isProcessing = true;
        //        while (_routing && isProcessing)
        //        {
        //            byte[] message = socket.Recv();
        //            if (IsStopCommand(message))
        //            {
        //                _routing = false;
        //                break;
        //            }
        //            else
        //            {
        //                _backendSocket.Send(message, socket.RcvMore ? SendRecvOpt.SNDMORE : 0);
        //                isProcessing = socket.RcvMore;
        //            }
        //        }
        //    }
        //    catch (ZMQ.Exception ex)
        //    {
        //        Logger.Instance.Debug(ex);
        //    }
        //}

        //private void BackendPollInHandler(ZmqSocket socket, IOMultiPlex revents)
        //{
        //    //  Send messages from backend worker to client
        //    try
        //    {
        //        bool isProcessing = true;
        //        while (_routing && isProcessing)
        //        {
        //            byte[] message = socket.Recv();
        //            if (IsStopCommand(message))
        //            {
        //                _routing = false;
        //                break;
        //            }
        //            else
        //            {
        //                _frontendSocket.Send(message, socket.RcvMore ? SendRecvOpt.SNDMORE : 0);
        //                isProcessing = socket.RcvMore;
        //            }
        //        }
        //    }
        //    catch (ZMQ.Exception ex)
        //    {
        //        Logger.Instance.Debug(ex);
        //    }
        //}

        private void StopRouting()
        {
            using (var sockFrontEnd = ServiceContext.CreateSocket(SocketType.REQ))
            using (var sockBackEnd = ServiceContext.CreateSocket(SocketType.REQ))
            {
                sockFrontEnd.Linger = TimeSpan.FromMilliseconds(0);
                sockBackEnd.Linger = TimeSpan.FromMilliseconds(0);

                sockFrontEnd.Connect(String.Format("tcp://{0}:{1}", "localhost", _frontendPort));
                sockFrontEnd.Send(_stopCommandToken, Encoding.UTF8);
                sockBackEnd.Connect(String.Format("tcp://{0}:{1}", "localhost", _backendPort));
                sockBackEnd.Send(_stopCommandToken, Encoding.UTF8);
            }
        }

        public override void Start()
        {
            StartRouting();
        }

        public override void Stop()
        {
            StopRouting();
        }
    }
}
