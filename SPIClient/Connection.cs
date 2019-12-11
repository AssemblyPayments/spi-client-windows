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
        
        private EventHandler<MessageEventArgs> _messageReceived;
        public event EventHandler<MessageEventArgs> MessageReceived
        {
            add { _messageReceived = _messageReceived + value; }
            remove { _messageReceived = _messageReceived - value; }
        }

        private EventHandler<ConnectionStateEventArgs> _connectionStatusChanged;
        public event EventHandler<ConnectionStateEventArgs> ConnectionStatusChanged
        {
            add { _connectionStatusChanged = _connectionStatusChanged + value; }
            remove { _connectionStatusChanged = _connectionStatusChanged - value; }
        }
        
        private EventHandler<MessageEventArgs> _errorReceived;
        public event EventHandler<MessageEventArgs> ErrorReceived
        {
            add { _errorReceived = _errorReceived + value; }
            remove { _errorReceived = _errorReceived - value; }
        }
        
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
            
            // Create a new socket instance specifying the url, SPI protocol and Websocket to use.
            // The will create a TCP/IP socket connection to the provided URL and perform HTTP websocket negotiation
            _ws = new WebSocket(Address, "spi.2.7.0", WebSocketVersion.Rfc6455);

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
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Thread.Sleep(4000);
                if (State == ConnectionState.Connecting)
                {
                    Disconnect();
                }
            }).Start();
            
            // Let's let our users know that we are now connecting...
            _connectionStatusChanged(this, new ConnectionStateEventArgs {ConnectionState = ConnectionState.Connecting});
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
            _connectionStatusChanged(sender, new ConnectionStateEventArgs {ConnectionState = ConnectionState.Connected});
        }

        private void _onClosed(object sender, EventArgs e)
        {
            Connected = false;
            State = ConnectionState.Disconnected;
            _ws.Opened -= _onOpened;
            _ws.Closed -= _onClosed;
            _ws.Dispose();
            _ws = null;
            _connectionStatusChanged(sender, new ConnectionStateEventArgs {ConnectionState = ConnectionState.Disconnected});
        }
        
        private void _onMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _messageReceived(sender, new MessageEventArgs{Message = e.Message});
        }

        private void _onError(object sender, ErrorEventArgs e)
        {
            _errorReceived(sender, new MessageEventArgs{Message = e.Exception.Message});
        }

    }
}