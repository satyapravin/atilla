using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using WebSocket4Net;
using WebSocket4Net.Common;

namespace BitmexWebSocket
{
    public interface IWebSocket : IDisposable
    {
        public DateTime LastActiveTime { get; }
        public WebSocketVersion Version { get; }
        public bool SupportBinary { get; }
        public WebSocketState State { get; }
        public bool Handshaked { get; }
        public IProxyConnector Proxy { get; set; }
        public int AutoSendPingInterval { get; set; }
        public bool EnableAutoSendPing { get; set; }
        public EndPoint LocalEndPoint { get; set; }
        public SecurityOption Security { get; }
        public int ReceiveBufferSize { get; set; }
        public bool NoDelay { get; set; }

        public event EventHandler Opened;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ErrorEventArgs> Error;
        public event EventHandler Closed;

        public void Close(int statusCode, string reason);
        public void Close(string reason);
        public void Close();
        public Task<bool> CloseAsync();
        public void Open();
        public Task<bool> OpenAsync();
        public void Send(IList<ArraySegment<byte>> segments);
        public void Send(byte[] data, int offset, int length);
        public void Send(string message);
    }
}
