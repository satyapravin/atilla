using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Text;
using WebSocket4Net;

namespace BitmexWebSocket
{
    public interface IWebSocketFactory
    {
        public IWebSocket Create(string uri,
                                string subProtocol = "",
                                List<KeyValuePair<string, string>> cookies = null,
                                List<KeyValuePair<string, string>> customHeaderItems = null,
                                string userAgent = "",
                                string origin = "",
                                WebSocketVersion version = WebSocketVersion.None,
                                EndPoint httpConnectProxy = null,
                                SslProtocols sslProtocols = SslProtocols.None,
                                int receiveBufferSize = 0);
    }
}
