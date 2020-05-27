using Newtonsoft.Json;

namespace BitmexWebSocket.Models.Socket
{
    public class BitmexWebSocketConnectionLimitMessage
    {
        [JsonProperty("remaining")]
        public int Remaining { get; set; }
    }
}
