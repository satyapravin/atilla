using NUnit.Framework;
using Moq;
using BitmexWebSocket;
using WebSocket4Net;
using BitmexCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using BitmexWebSocket.Dtos.Socket;
using Newtonsoft.Json;
using BitmexWebSocket.Models.Socket;
using TestsBase;

namespace WebSocketTests
{
    public class TestWebSockets
    {
        [Test]
        public void TestCreationWithoutAuthorize()
        {
            Create(false, false, false);
            Create(false, true, false);
        }

        [Test]
        public void TestCreationWithAuthorize()
        {
            Create(true, false, false);
            Create(true, true, false);
            Create(true, true, true);
        }

        public void Create(bool authorize, bool succeed, bool authsucceed)
        {
            var mocks = SocketTestBase.GetSocketMocks();
            var sfactory = mocks.Item1;
            var socket = mocks.Item2;
            socket.Setup(x => x.Open());
            var svc = new BitmexApiSocketService(new BitmexAuthorization()
            {
                BitmexEnvironment = BitmexCore.Models.BitmexEnvironment.Test,
                Key = "Key",
                Secret = "Secret"
            }, sfactory.Object, new NullLoggerFactory());

            var wargs = new BitmexWelcomeMessage()
            {
                Docs = string.Empty,
                Info = string.Empty,
                Limit = new BitmexWebSocketConnectionLimitMessage() { Remaining = succeed ? 20 : 0 },
                Timestamp = DateTime.UtcNow,
                Version = "v1"
            };

            socket.Setup(x => x.Open()).Raises(x => x.MessageReceived += null,
                                               new MessageReceivedEventArgs(JsonConvert.SerializeObject(wargs)));

            if (authorize)
            {
                var args = new BitmexSocketOperationResultDto();
                args.Error = string.Empty;
                args.Status = "success";
                args.Request = new InitialRequstInfoDto()
                {
                    Arguments = new string[] { "arg1", "arg2" },
                    Operation = OperationType.authKeyExpires
                };
                args.Success = authsucceed;

                socket.Setup(x => x.Send(It.IsAny<string>())).Raises(x => x.MessageReceived += null,
                                     new MessageReceivedEventArgs(JsonConvert.SerializeObject(args)));
            }

            socket.SetupGet(y => y.State).Returns(WebSocketState.Open);


            if ((authsucceed && succeed) || (succeed && !authorize))
            {
                bool retval = svc.Connect(authorize);
                Assert.AreEqual(authsucceed, svc.IsAuthorized);
                Assert.AreEqual(succeed, retval);
                socket.Verify(x => x.Open(), Times.Once());
            }
            else if (succeed && authorize && !authsucceed)
            {
                Assert.Throws<BitmexSocketAuthorizationException>(() => svc.Connect(authorize));
            }        
            else if (!succeed)
            {
                Assert.Throws<BitmexWebSocketLimitReachedException>(() => svc.Connect(authorize));
            }

            if (authorize && succeed)
            {
                socket.Verify(x => x.Send(It.IsAny<string>()), Times.Once());
            }
        }
    }
}