using AtillaCore;
using NUnit.Framework;
using Moq;
using TestsBase;
using StrategyCore;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using BitmexCore.Dtos;
using System;
using BitmexWebSocket;
using System.Net.Sockets;
using BitmexWebSocket.Dtos.Socket;
using System.Threading;

namespace AtillaTests
{
    public class Tests
    {
        private AtillaConfig config;
        private CorrQuantStrategyService strategy;
        private Tuple<Mock<IWebSocketFactory>, Mock<IWebSocket>> wtuple;

        [SetUp]
        public void Setup()
        {
            config = new AtillaConfig();
            config.Bitmex = new BitmexCore.BitmexConfig
            {
                IsProd = false,
                Key = "key",
                Secret = "secret"
            };
            config.ETHBTCQuantity = 10;

            var ethdef = new InstrumentDef("ETH", "ETHUSD", 0.05m);
            var btcdef = new InstrumentDef("BTC", "XBTUSD", 0.05m);
            var ethfutdef = new InstrumentDef("ETHFuture", "ETHUSDM20", 0.05m);
            var btcfutdef = new InstrumentDef("BTCFuture", "XBTM20", 0.05m);
            var ethxbtdef = new InstrumentDef("ETHBTCFuture", "ETHM20", 0.00001m);
            config.Instruments = new StrategyCore.InstrumentConfig
            {
                InstrumentDefs = new Dictionary<string, InstrumentDef>()
            {
                {ethdef.Name, ethdef },
                {btcdef.Name, btcdef },
                {ethfutdef.Name, ethfutdef },
                { btcfutdef.Name, btcfutdef },
                {ethxbtdef.Name, ethxbtdef }
            }
            };
            config.RebalanceInterval = 60 * 60 * 1000;
        }

        [Test]
        public void TestCreation()
        {
            var httpHandler = RestTestBase.GetMockHttpHandler();
            wtuple = SocketTestBase.GetSocketMocks();
            SocketTestBase.SetSocketForOpen(wtuple.Item2);
            SocketTestBase.SetSocketForSendAuthorize(wtuple.Item2);
            SocketTestBase.SetSocketForSendSubscribe(wtuple.Item2, new string[]
            { "orderBook10:XBTUSD", "orderBook10:ETHUSD", "orderBook10:XBTM20", "orderBook10:ETHM20", "orderBook10:ETHUSDM20" });
            strategy = new CorrQuantStrategyService(config, wtuple.Item1.Object, httpHandler.Object, new NullLoggerFactory());
            Assert.IsTrue(strategy.GetWebSocketService().Status);
            Assert.IsTrue(strategy.GetInstrumentService().Status);
            Assert.IsTrue(strategy.GetMarketDataService().Status);
            Assert.IsTrue(strategy.GetOrderService().Status);
            Assert.IsTrue(strategy.GetPositionService().Status);

            Assert.IsFalse(strategy.Status);
            Assert.IsFalse(strategy.GetETHBTCQuoter().Status);
            Assert.IsFalse(strategy.GetETHHedger().Status);
            Assert.IsFalse(strategy.GetBTCHedger().Status);
            Assert.IsFalse(strategy.GetRebalancer().Status);
        }

        [Test]
        public void TestStart()
        {
            SocketTestBase.SetSocketForSendSubscribe(wtuple.Item2, new string[] { "order" });
            SocketTestBase.SetSocketForSendSubscribe(wtuple.Item2, new string[] { "position" });
            strategy.Start();
            Assert.IsTrue(strategy.Status);
            Assert.IsTrue(strategy.GetETHBTCQuoter().Status);
            Assert.IsTrue(strategy.GetETHHedger().Status);
            Assert.IsTrue(strategy.GetBTCHedger().Status);
            Assert.IsTrue(strategy.GetRebalancer().Status);

            Assert.IsTrue(strategy.GetWebSocketService().Status);
            Assert.IsTrue(strategy.GetInstrumentService().Status);
            Assert.IsTrue(strategy.GetMarketDataService().Status);
            Assert.IsTrue(strategy.GetOrderService().Status);
            Assert.IsTrue(strategy.GetPositionService().Status);
        }

        [Test]
        public void TestStop()
        {
            strategy.Stop();

            Assert.IsFalse(strategy.Status);
            Assert.IsFalse(strategy.GetETHBTCQuoter().Status);
            Assert.IsFalse(strategy.GetETHHedger().Status);
            Assert.IsFalse(strategy.GetBTCHedger().Status);
            Assert.IsFalse(strategy.GetRebalancer().Status);

            Assert.IsTrue(strategy.GetWebSocketService().Status);
            Assert.IsTrue(strategy.GetInstrumentService().Status);
            Assert.IsTrue(strategy.GetMarketDataService().Status);
            Assert.IsTrue(strategy.GetOrderService().Status);
            Assert.IsTrue(strategy.GetPositionService().Status);
        }

        [Test]
        public void TestRestartAndQuote()
        {
            wtuple.Item2.Verify(x => x.Send(It.IsAny<string>()), Times.AtMost(8)); // 8 times from start of this class
            strategy.Start();
            Assert.IsTrue(strategy.Status);
            Assert.IsTrue(strategy.GetETHBTCQuoter().Status);
            Assert.IsTrue(strategy.GetETHHedger().Status);
            Assert.IsTrue(strategy.GetBTCHedger().Status);
            Assert.IsTrue(strategy.GetRebalancer().Status);

            Assert.IsTrue(strategy.GetWebSocketService().Status);
            Assert.IsTrue(strategy.GetInstrumentService().Status);
            Assert.IsTrue(strategy.GetMarketDataService().Status);
            Assert.IsTrue(strategy.GetOrderService().Status);
            Assert.IsTrue(strategy.GetPositionService().Status);

            OrderBook10Dto[] orderBookData = new OrderBook10Dto[]
            {
                new OrderBook10Dto
                {
                    Asks = new decimal[][] { new decimal[] { 100, 1 }, new decimal[] { 101, 1 } },
                    Bids = new decimal[][] { new decimal[] { 98, 1 }, new decimal[] { 99, 1 } },
                    Symbol = "ETHUSD",
                    Timestamp = new DateTimeOffset(DateTime.UtcNow),
                },
                new OrderBook10Dto
                {
                    Asks = new decimal[][] { new decimal[] { 10000, 1 }, new decimal[] { 10001, 1 } },
                    Bids = new decimal[][] { new decimal[] { 9998, 1 }, new decimal[] { 9999, 1 } },
                    Symbol = "XBTUSD",
                    Timestamp = new DateTimeOffset(DateTime.UtcNow),
                },
                new OrderBook10Dto
                {
                    Asks = new decimal[][] { new decimal[] { 110, 1 }, new decimal[] { 111, 1 } },
                    Bids = new decimal[][] { new decimal[] { 108, 1 }, new decimal[] { 109, 1 } },
                    Symbol = "ETHUSDM20",
                    Timestamp = new DateTimeOffset(DateTime.UtcNow),
                },
                new OrderBook10Dto
                {
                    Asks = new decimal[][] { new decimal[] { 10040, 1 }, new decimal[] { 10041, 1 } },
                    Bids = new decimal[][] { new decimal[] { 10039, 1 }, new decimal[] { 10038, 1 } },
                    Symbol = "XBTM20",
                    Timestamp = new DateTimeOffset(DateTime.UtcNow),
                },
                new OrderBook10Dto
                {
                    Asks = new decimal[][] { new decimal[] { 0.01002m, 1 }, new decimal[] { 0.01001m, 1 } },
                    Bids = new decimal[][] { new decimal[] { 0.01m, 1 }, new decimal[] { 0.00999m, 1 } },
                    Symbol = "ETHM20",
                    Timestamp = new DateTimeOffset(DateTime.UtcNow),
                }
            };
            
            SocketTestBase.SetSocketForReceiving(wtuple.Item2, orderBookData);
            Thread.Sleep(6000);
            wtuple.Item2.Verify(x => x.Send(It.IsAny<string>()), Times.AtMost(10));
            wtuple.Item2.Verify(x => x.Send(It.IsAny<string>()), Times.AtLeast(10));
        }
    }
}