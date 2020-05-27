using System;
using TestsBase;
using BitmexCore;
using NUnit.Framework;
using ExchangeCore;
using BitmexCore.Models;
using Microsoft.Extensions.Logging.Abstractions;
using BitmexWebSocket.Dtos.Socket;
using System.Threading;
using BitmexRESTApi;
using BitmexCore.Dtos;
using System.Net.Sockets;

namespace ExchangeCoreTests
{
    public class PositionServiceTests
    {
        [Test]
        public void TestCreation()
        {
            var httpHandler = RestTestBase.GetMockHttpHandler();

            var auth = new BitmexAuthorization()
            {
                Key = "key",
                Secret = "secret",
                BitmexEnvironment = BitmexEnvironment.Test
            };

            var restSvc = BitmexApiService.CreateDefaultApi(auth, httpHandler.Object, new NullLoggerFactory());

            var webTuple = SocketTestBase.GetSocketMocks();
            var webSvc = new WebSocketService(auth, webTuple.Item1.Object, new NullLoggerFactory());
            SocketTestBase.SetSocketForOpen(webTuple.Item2);
            SocketTestBase.SetSocketForSendAuthorize(webTuple.Item2);
            webSvc.Start();
            IPMS posvc = new PositionService(webSvc, restSvc, new NullLoggerFactory());
            Assert.Throws<ApplicationException>(() => posvc.GetPosition("ETHUSD"));
            SocketTestBase.SetSocketForSendSubscribe(webTuple.Item2, new string[] { "position" });
            posvc.Start();
            Assert.IsTrue(posvc.Status);
            Assert.DoesNotThrow(() => posvc.GetPosition("ETHUSD"));
            Assert.AreEqual(null, posvc.GetPosition("ETHUSD"));
            var posData = new PositionDto[1];
            var position = PositionTestBase.GetDummyPosition();
            position.Symbol = "ETHUSD";
            posData[0] = position;
            SocketTestBase.SetSocketForReceiving(webTuple.Item2, posData, BitmexActions.Partial);
            Thread.Sleep(2000);
            var actual = posvc.GetPosition("ETHUSD");
            PositionTestBase.Compare(position, actual);
            SocketTestBase.Closed(webTuple.Item2);
            Assert.Throws<ApplicationException>(() => posvc.GetPosition("ETHUSD"));
        }
    }
}
