using AtillaCore;
using NUnit.Framework;
using Moq;
using TestsBase;
using StrategyCore;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using BitmexWebSocket;
using BitmexWebSocket.Dtos.Socket;
using System.Threading;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
using BitmexCore.Models;
using System.Linq;
using BitmexCore.Dtos;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;

namespace AtillaTests
{
    public class Tests
    {
        private AtillaConfig config;
        private CorrQuantStrategyService strategy;
        private Tuple<Mock<IWebSocketFactory>, Mock<IWebSocket>> wtuple;
        private Mock<HttpMessageHandler> httpHandler;

        [SetUp]
        public void Setup()
        {
            config = new AtillaConfig
            {
                Bitmex = new BitmexCore.BitmexConfig
                {
                    IsProd = false,
                    Key = "key",
                    Secret = "secret"
                },
                ETHBTCQuantity = 10
            };

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
            File.WriteAllText(@"C:\Pravin\AtillaConfig.json", JsonConvert.SerializeObject(config));
        }

        [Test]
        public void TestCreation()
        {
            httpHandler = RestTestBase.GetMockHttpHandler();
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

            var msg = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(string.Empty)
            };

            var reqTxt = new List<string>();
            int id = 1;
            var orderMap = new Dictionary<string, OrderDto>();

            RestTestBase.SetupForResponse(httpHandler, msg,
                (HttpRequestMessage reqmsg, CancellationToken token) =>
                {
                    if (reqmsg.Content != null)
                    {
                        var txt = reqmsg.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        reqTxt.Add(txt);
                        var reqs = JsonConvert.DeserializeObject<OrderBulkPOSTRequestParams>(txt);

                        if (reqs.Orders != null && reqmsg.Method == HttpMethod.Post)
                        {
                            var orders = new OrderDto[2];
                            orders[0] = OrderTestBase.GetDummyOrder();
                            orders[1] = OrderTestBase.GetDummyOrder();
                            orders[0].OrderId = id++.ToString();
                            orders[1].OrderId = id++.ToString();
                            orders[0].Symbol = reqs.Orders[0].Symbol;
                            orders[1].Symbol = reqs.Orders[1].Symbol;
                            orders[0].Side = reqs.Orders[0].Side;
                            orders[1].Side = reqs.Orders[1].Side;
                            orders[0].OrderQty = reqs.Orders[0].OrderQty;
                            orders[1].OrderQty = reqs.Orders[1].OrderQty;
                            orders[0].Price = reqs.Orders[0].Price;
                            orders[1].Price = reqs.Orders[1].Price;
                            lock (orderMap)
                            {
                                orderMap[orders[0].OrderId] = orders[0];
                                orderMap[orders[1].OrderId] = orders[1];
                            }
                            msg.Content = new StringContent(JsonConvert.SerializeObject(orders));
                        }
                        else if (reqmsg.Method == HttpMethod.Post)
                        {
                            var req = JsonConvert.DeserializeObject<OrderPOSTRequestParams>(txt);
                            var order = OrderTestBase.GetDummyOrder();
                            order.OrderId = id++.ToString();
                            order.Symbol = req.Symbol;
                            order.Side = req.Side;
                            order.OrderQty = req.OrderQty;
                            order.Price = req.Price;
                            lock (orderMap)
                            {
                                orderMap[order.OrderId] = order;
                            }
                            msg.Content = new StringContent(JsonConvert.SerializeObject(order));
                        }
                        else if (reqmsg.Method == HttpMethod.Put)
                        {
                            var puts = JsonConvert.DeserializeObject<OrderBulkPUTRequestParams>(txt);

                            if (puts.Orders != null)
                            {
                                var dtos = new List<OrderDto>();
                                foreach (var put in puts.Orders)
                                {
                                    OrderDto order = null;
                                    lock (orderMap)
                                    {
                                        order = orderMap[put.OrderID];
                                    }

                                    order.OrderQty = put.OrderQty;
                                    dtos.Append(order);
                                }

                                msg.Content = new StringContent(JsonConvert.SerializeObject(dtos.ToArray()));
                            }
                            else
                            {
                                var put = JsonConvert.DeserializeObject<OrderPUTRequestParams>(txt);
                                OrderDto order = orderMap[put.OrderID];
                                if (put.OrderQty != null)
                                    order.OrderQty = put.OrderQty;
                                if (put.Price != null)
                                    order.Price = put.Price;
                                msg.Content = new StringContent(JsonConvert.SerializeObject(order));
                            }
                        }
                    }
                });


            SocketTestBase.SetSocketForReceiving(wtuple.Item2, orderBookData);
            Thread.Sleep(5000);
            wtuple.Item2.Verify(x => x.Send(It.IsAny<string>()), Times.AtMost(8));
            wtuple.Item2.Verify(x => x.Send(It.IsAny<string>()), Times.AtLeast(8));
            var reqs = JsonConvert.DeserializeObject<OrderBulkPOSTRequestParams>(reqTxt[0]);
            Assert.AreEqual(2, reqs.Orders.Count());
            OrderPOSTRequestParams bid, ask;

            if (reqs.Orders[0].Side.Equals("Buy"))
            {
                bid = reqs.Orders[0];
                ask = reqs.Orders[1];
            }
            else
            {
                bid = reqs.Orders[1];
                ask = reqs.Orders[0];
            }

            Assert.AreEqual("ETHM20", bid.Symbol);
            Assert.AreEqual(0.00985m, bid.Price);
            Assert.AreEqual("Limit", bid.OrdType);
            Assert.AreEqual(10, bid.OrderQty);

            Assert.AreEqual("ETHM20", ask.Symbol);
            Assert.AreEqual(0.01001m, ask.Price);
            Assert.AreEqual("Limit", ask.OrdType);
            Assert.AreEqual(10, ask.OrderQty);
            strategy.SetBaseETHBTCQuantity(5);
            Thread.Sleep(10000);

            List<OrderDto> ordlst;
            lock(orderMap)
            {
                ordlst = orderMap.Values.ToList();
            }

            bool bidRet = false;
            bool askRet = false;
            foreach (var ord in ordlst)
            {
                if (ord.Symbol.Equals("ETHM20") && ord.Side.Equals("Buy") && ord.OrderQty == 5m)
                {
                    bidRet = true;
                }
                else if (ord.Symbol.Equals("ETHM20") && ord.Side.Equals("Sell") && ord.OrderQty == 5m)
                {
                    askRet = true;
                }
            }

            Assert.IsTrue(bidRet);
            Assert.IsTrue(askRet);

            var pos = PositionTestBase.GetDummyPosition();
            pos.Symbol = "ETHM20";
            pos.CurrentQty = 10;
            PositionDto[] parray = new PositionDto[1] { pos };

            SocketTestBase.SetSocketForReceiving(wtuple.Item2, parray, BitmexActions.Partial);
            Thread.Sleep(30000);
            
            lock(orderMap)
            {
                ordlst = orderMap.Values.ToList();
            }

            bool ethplaced = false;
            bool xbtplaced = false;

            foreach(var ord in ordlst)
            {
                if (ord.Symbol == "ETHUSDM20") ethplaced = true;
                if (ord.Symbol == "XBTUSD") xbtplaced = true;
            }

            Assert.IsTrue(ethplaced);
            Assert.IsTrue(xbtplaced);
        }
    }
}