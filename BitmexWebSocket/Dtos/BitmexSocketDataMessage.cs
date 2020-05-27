using System.Diagnostics;

namespace BitmexWebSocket.Dtos.Socket
{
    [DebuggerDisplay("{" + nameof(Action) + "}")]
    public class BitmexSocketDataMessage<T>
    {
        public BitmexSocketDataMessage(BitmexActions action, T data)
        {
            this.Action = action;
            this.Data = data;
        }

        public BitmexActions Action { get; }

        public T Data { get; }
    }
}
