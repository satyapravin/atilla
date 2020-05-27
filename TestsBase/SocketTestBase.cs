using BitmexCore.Dtos;
using BitmexWebSocket;
using BitmexWebSocket.Dtos.Socket;
using BitmexWebSocket.Models.Socket;
using Castle.Core.Resource;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using WebSocket4Net;

namespace TestsBase
{
    public class SocketTestBase
    {
        public static Tuple<Mock<IWebSocketFactory>, Mock<IWebSocket>> GetSocketMocks()
        {
            var socket = new Mock<IWebSocket>();
            var sfactory = new Mock<IWebSocketFactory>();
            sfactory.Setup(x => x.Create(It.IsAny<string>(),
                                         It.IsAny<string>(),
                                         It.IsAny<List<KeyValuePair<string, string>>>(),
                                         It.IsAny<List<KeyValuePair<string, string>>>(),
                                         It.IsAny<string>(),
                                         It.IsAny<string>(),
                                         It.IsAny<WebSocketVersion>(),
                                         It.IsAny<EndPoint>(),
                                         It.IsAny<SslProtocols>(),
                                         It.IsAny<int>())).Returns(socket.Object);
            return new Tuple<Mock<IWebSocketFactory>, Mock<IWebSocket>>(sfactory, socket);
        }

        public static void SetSocketForOpen(Mock<IWebSocket> socket)
        {
            var wargs = new BitmexWelcomeMessage()
            {
                Docs = string.Empty,
                Info = string.Empty,
                Limit = new BitmexWebSocketConnectionLimitMessage() { Remaining = 10 },
                Timestamp = DateTime.UtcNow,
                Version = "v1"
            };

            socket.Setup(x => x.Open()).Raises(x => x.MessageReceived += null,
                                               new MessageReceivedEventArgs(JsonConvert.SerializeObject(wargs)));
        }

        public static void SetSocketForSendAuthorize(Mock<IWebSocket> socket)
        {
            var args = new BitmexSocketOperationResultDto();
            args.Error = string.Empty;
            args.Status = "success";
            args.Request = new InitialRequstInfoDto()
            {
                Arguments = new string[] { "arg1", "arg2" },
                Operation = OperationType.authKeyExpires,
            };
            args.Success = true;
            socket.Setup(x => x.Send(It.Is<string>((x) => x.Contains("authKeyExpires")))).Raises(x => x.MessageReceived += null,
                     new MessageReceivedEventArgs(JsonConvert.SerializeObject(args)));

            socket.SetupGet(y => y.State).Returns(WebSocketState.Open);

        }

        public static void SetSocketForSendSubscribe(Mock<IWebSocket> socket, string[] strArgs)
        {
            var args = new BitmexSocketOperationResultDto();
            args.Error = string.Empty;
            args.Status = "success";
            args.Request = new InitialRequstInfoDto()
            {
                Arguments = strArgs,
                Operation = OperationType.subscribe,
            };
            args.Success = true;

            socket.Setup(x => x.Send(It.Is<string>(x => x.Contains("subscribe")))).Raises(x => x.MessageReceived += null,
                                     new MessageReceivedEventArgs(JsonConvert.SerializeObject(args)));
        }

        public static void SetSocketForReceiving(Mock<IWebSocket> socket, OrderBook10Dto[] obdata)
        {
            var data = new BitmexSocketDataDto()
            {
                Action = BitmexActions.Partial,
                TableName = "orderBook10",
                AdditionalData = new Dictionary<string, JToken>() { { "data", JArray.Parse(JsonConvert.SerializeObject(obdata)) } }
            };

            Task t = new Task(() => 
                socket.Raise(x => x.MessageReceived += null, new MessageReceivedEventArgs(JsonConvert.SerializeObject(data))));
            t.Start();
        }

        public static void SetSocketForReceiving(Mock<IWebSocket> socket, PositionDto[] pdata, BitmexActions action)
        {
            var data = new BitmexSocketDataDto()
            {
                Action = action,
                TableName = "position",
                AdditionalData = new Dictionary<string, JToken>() { { "data", JArray.Parse(JsonConvert.SerializeObject(pdata)) } }
            };

            socket.Raise(x => x.MessageReceived += null, new MessageReceivedEventArgs(JsonConvert.SerializeObject(data)));
        }

        public static void SetSocketForReceiving(Mock<IWebSocket> socket, OrderDto[] pdata, BitmexActions action)
        {
            var data = new BitmexSocketDataDto()
            {
                Action = action,
                TableName = "order",
                AdditionalData = new Dictionary<string, JToken>() { { "data", JArray.Parse(JsonConvert.SerializeObject(pdata)) } }
            };

            socket.Raise(x => x.MessageReceived += null, new MessageReceivedEventArgs(JsonConvert.SerializeObject(data)));
        }

        public static void SetSocketForReceiving(Mock<IWebSocket> socket, SettlementDto[] pdata, BitmexActions action)
        {
            var data = new BitmexSocketDataDto()
            {
                Action = action,
                TableName = "settlement",
                AdditionalData = new Dictionary<string, JToken>() { { "data", JArray.Parse(JsonConvert.SerializeObject(pdata)) } }
            };

            socket.Raise(x => x.MessageReceived += null, new MessageReceivedEventArgs(JsonConvert.SerializeObject(data)));
        }

        public static void SetSocketForReceiving(Mock<IWebSocket> socket, LiquidationDto[] pdata, BitmexActions action)
        {
            var data = new BitmexSocketDataDto()
            {
                Action = action,
                TableName = "liquidation",
                AdditionalData = new Dictionary<string, JToken>() { { "data", JArray.Parse(JsonConvert.SerializeObject(pdata)) } }
            };

            socket.Raise(x => x.MessageReceived += null, new MessageReceivedEventArgs(JsonConvert.SerializeObject(data)));
        }

        public static void Closed(Mock<IWebSocket> socket)
        {
            socket.Raise(x => x.Closed += null, new EventArgs());
        }
    }
}
