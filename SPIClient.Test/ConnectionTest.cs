using SPIClient;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Test
{
    public class ConnectionTest
    {
        [Fact]
        public void ConnectionStateEventArgs_OnDisconnected_ReturnStatus()
        {
            // arrange
            var are = new AutoResetEvent(false);
            var conn = new Connection();
            conn.Address = "ws://127.0.0.1";

            // act
            var connectionStateEventArgs = new List<ConnectionStateEventArgs>();
            conn.ConnectionStatusChanged += delegate (object sender, ConnectionStateEventArgs state)
            {
                connectionStateEventArgs.Add(state);
            };
            conn.ErrorReceived += delegate (object sender, MessageEventArgs error)
            {
                // assert
                Assert.NotNull(error.Message);
            };
            conn.Connect();
            var wasSignaled = are.WaitOne(timeout: TimeSpan.FromSeconds(9));

            // assert
            Assert.Equal(ConnectionState.Connecting, connectionStateEventArgs[0].ConnectionState);
            Assert.Equal(ConnectionState.Disconnected, connectionStateEventArgs[1].ConnectionState);
            Assert.False(conn.Connected);
        }
    }
}
