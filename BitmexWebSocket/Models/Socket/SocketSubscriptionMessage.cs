namespace BitmexWebSocket.Models.Socket
{
	public sealed class SocketSubscriptionMessage : SocketMessage
	{
		public SocketSubscriptionMessage(params object[] args) : base(OperationType.subscribe, args)
		{
		}
	}
}
