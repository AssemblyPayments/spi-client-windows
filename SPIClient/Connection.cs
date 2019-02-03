using System;
using System.Threading;
using WebSocket4Net;
using SuperSocket.ClientEngine;


namespace SPIClient
{
    public enum ConnectionState
    {
        Connected,
        Disconnected,
        Connecting
    };

    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionState ConnectionState { get; internal set; }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; internal set; }
    }

    public class Connection
    {
        public ConnectionState State { get; private set; }
        public bool Connected { get; private set; }
        private EventWaitHandle _connectingWaitHandle;

        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<ConnectionStateEventArgs> ConnectionStatusChanged;
        public event EventHandler<MessageEventArgs> ErrorReceived;

        public string Address { get; set; }
        private WebSocket _ws;

        public Connection()
        {
            State = ConnectionState.Disconnected;
        }
        
        public void Connect()
        {
            if (State == ConnectionState.Connected || State == ConnectionState.Connecting)
            {
                // already connected or connecting. disconnect first.
                return;
            }
            
            //Create a new socket instance specifying the url, SPI protocol and Websocket to use.
            //The will create a TCP/IP socket connection to the provided URL and perform HTTP websocket negotiation
            _ws = new WebSocket(Address, "spi.2.4.0", WebSocketVersion.Rfc6455);
            _connectingWaitHandle = new ManualResetEvent(false);

            // Setup event handling
            _ws.Opened += _onOpened;
            _ws.Closed += _onClosed;
            _ws.MessageReceived += _onMessageReceived;
            _ws.Error += _onError;
            
            State = ConnectionState.Connecting;
            
            // This one is non-blocking
            _ws.Open();
            
            // We have noticed that sometimes this websocket library, even when the network connectivivity is back,
            // it never recovers nor gives up. So here is a crude way of timing out after 8 seconds.

            var disconnectIfStillConnecting = new Action(() => {
                if (State == ConnectionState.Connecting)
                {
                    Disconnect();
                }
            });
            ThreadPool.RegisterWaitForSingleObject(_connectingWaitHandle, (o, b) => disconnectIfStillConnecting(), null, 8000, true);

            //The original code throws an exception, (nullpointerException) if no handler registered
            // Let's let our users know that we are now connecting...
            ConnectionStatusChanged?.Invoke(this, new ConnectionStateEventArgs { ConnectionState = ConnectionState.Connecting });
        }

        public void Disconnect()
        {
            var ws = _ws;
            if (ws != null && ws.State != WebSocketState.Closed)
                ws.Close();
        }
        
        public void Send(string message)
        {
            _ws.Send(message);
        } 

        private void _onOpened(object sender, EventArgs e)
        {
            State = ConnectionState.Connected;
            Connected = true;
            _connectingWaitHandle.Set();
            ConnectionStatusChanged?.Invoke(sender, new ConnectionStateEventArgs { ConnectionState = ConnectionState.Connected });
        }

        private void _onClosed(object sender, EventArgs e)
        {
            Connected = false;
            State = ConnectionState.Disconnected;
            _ws.Opened -= _onOpened;
            _ws.Closed -= _onClosed;
            _ws.Dispose();
            _ws = null;
            _connectingWaitHandle = null;
            ConnectionStatusChanged?.Invoke(sender, new ConnectionStateEventArgs { ConnectionState = ConnectionState.Disconnected });
        }
        
        private void _onMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //Null reference exception avoided
            MessageReceived?.Invoke(sender, new MessageEventArgs { Message = e.Message });
        }

        private void _onError(object sender, ErrorEventArgs e)
        {
            //The original code throws another exception, (nullpointerException) if an error occurs and no handler registered
            ErrorReceived?.Invoke(sender, new MessageEventArgs { Message = e.Exception.Message });
        }

    }
}