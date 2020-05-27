using System;
using TestsBase;
using Moq;
using BitmexCore;
using NUnit.Framework;
using ExchangeCore;
using BitmexCore.Models;
using BitmexWebSocket.Models.Socket;
using Microsoft.Extensions.Logging.Abstractions;
using BitmexWebSocket;
using Newtonsoft.Json;
using WebSocket4Net;
using BitmexWebSocket.Dtos.Socket;

namespace ExchangeCoreTests
{
    public class WebSocketServiceTests
    {
        [Test]
        public void TestCreationAndSubscription()
        {
            var tpl = SocketTestBase.GetSocketMocks();
            var sf = tpl.Item1;
            var socket = tpl.Item2;
            var ws = new WebSocketService(new BitmexAuthorization
            { BitmexEnvironment = BitmexEnvironment.Test, Key = "k", Secret = "s" }, sf.Object
            , new NullLoggerFactory());
            Assert.IsFalse(ws.Status);
            Assert.Throws<ApplicationException>(() => ws.Subscribe(BitmexSocketSubscriptions.CreateOrderBook10Subsription(
                (o) => { }, new string[] { "XBTUSD", "ETHUSD" })));

            SocketTestBase.SetSocketForOpen(socket);
            SocketTestBase.SetSocketForSendAuthorize(socket);
            ws.Start();
            SocketTestBase.SetSocketForSendSubscribe(socket, new string[] { "arg1", "arg2" });

            Assert.DoesNotThrow(() => ws.Subscribe(BitmexSocketSubscriptions.CreateOrderBook10Subsription(
                (o) => { }, new string[] { "XBTUSD", "ETHUSD" })));

        }
    }
}
