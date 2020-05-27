using System;
using TestsBase;
using BitmexCore;
using NUnit.Framework;
using ExchangeCore;
using BitmexCore.Models;
using Microsoft.Extensions.Logging.Abstractions;
using BitmexWebSocket.Dtos.Socket;
using System.Threading;

namespace ExchangeCoreTests
{
    class MarketDataServiceTests
    {
        [Test]
        public void TestCreationWithoutStartingWS()
        {
            var tpl = SocketTestBase.GetSocketMocks();
            var sf = tpl.Item1;
            var socket = tpl.Item2;
            var ws = new WebSocketService(new BitmexAuthorization
            { BitmexEnvironment = BitmexEnvironment.Test, Key = "k", Secret = "s" }, sf.Object
            , new NullLoggerFactory());

            IMDS mds = new MarketDataService(ws, new NullLoggerFactory());
            mds.Register(new string[] { "ETHUSD", "XBTUSD" });
            Assert.Throws<ApplicationException>(() => mds.Start());
        }

        [Test]
        public void TestCreationAfterStartingWS()
        {
            var tpl = SocketTestBase.GetSocketMocks();
            var sf = tpl.Item1;
            var socket = tpl.Item2;
            var ws = new WebSocketService(new BitmexAuthorization
            { BitmexEnvironment = BitmexEnvironment.Test, Key = "k", Secret = "s" }, sf.Object
            , new NullLoggerFactory());
            SocketTestBase.SetSocketForOpen(socket);
            SocketTestBase.SetSocketForSendAuthorize(socket);
            ws.Start();
            SocketTestBase.SetSocketForSendSubscribe(socket, new string[] { "orderBook10:XBTUSD", "orderBook10:ETHUSD" });

            IMDS mds = new MarketDataService(ws, new NullLoggerFactory());
            mds.Register(new string[] { "ETHUSD", "XBTUSD" });
            Assert.DoesNotThrow(() => mds.Start());
            Assert.Throws<ApplicationException>(() => mds.GetBidAsk("ETHUSD"));
            var obdata = new OrderBook10Dto[] {
                new OrderBook10Dto
                {
                    Asks = new decimal[][] { new decimal[] { 100, 1 }, new decimal[] { 101, 1 } },
                    Bids = new decimal[][] { new decimal[] { 98, 1 }, new decimal[] { 99, 1 } },
                    Symbol = "ETHUSD",
                    Timestamp = new DateTimeOffset(DateTime.UtcNow),
                }
            };
            SocketTestBase.SetSocketForReceiving(socket, obdata);
            Thread.Sleep(1000);
            var mdat = mds.GetBidAsk("ETHUSD");
            Assert.AreEqual(100, mdat.Item2);
            Assert.AreEqual(99, mdat.Item1);
            Assert.Throws<ApplicationException>(() => mds.GetBidAsk("XBTUSD"));
            obdata = new OrderBook10Dto[] {
                new OrderBook10Dto
                {
                    Asks = new decimal[][] { new decimal[] { 200.2m, 1 }, new decimal[] { 200.1m, 1 } },
                    Bids = new decimal[][] { new decimal[] { 199.0m, 1 }, new decimal[] { 199.5m, 1 } },
                    Symbol = "ETHUSD",
                    Timestamp = new DateTimeOffset(DateTime.UtcNow),
                }
            };
            SocketTestBase.SetSocketForReceiving(socket, obdata);
            Thread.Sleep(1000);
            mdat = mds.GetBidAsk("ETHUSD");
            Assert.AreEqual(200.1m, mdat.Item2);
            Assert.AreEqual(199.5m, mdat.Item1);
            SocketTestBase.Closed(socket);
            Assert.Throws<ApplicationException>(() => mds.GetBidAsk("ETHUSD"));
        }
    }
}
