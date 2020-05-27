using System;

namespace BitmexWebSocket
{
    public class BitmexWebSocketLimitReachedException : Exception
    {
        public BitmexWebSocketLimitReachedException() : base("remining connections count is 0")
        {

        }
    }
}
