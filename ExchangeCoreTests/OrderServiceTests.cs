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

namespace ExchangeCoreTests
{
    public class OrderServiceTests
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
            IOMS omsvc = new OrderService(restSvc, webSvc, new NullLoggerFactory());
            SocketTestBase.SetSocketForSendSubscribe(webTuple.Item2, new string[] { "order" });
            omsvc.Start();
            Assert.IsTrue(omsvc.Status);
            Assert.IsTrue(webSvc.Status);
            Assert.AreEqual(omsvc.GetOpenOrders("ETHUSD").Count, 0);
            var orderData = new OrderDto[1];
            var order = OrderTestBase.GetDummyOrder();
            order.Symbol = "ETHUSD";
            order.WorkingIndicator = true;
            order.OrdStatus = "PartiallyFilled";
            orderData[0] = order;
            SocketTestBase.SetSocketForReceiving(webTuple.Item2, orderData, BitmexActions.Partial);
            Thread.Sleep(2000);
            Assert.AreEqual(1, omsvc.GetOpenOrders("ETHUSD").Count);
            OrderTestBase.Compare(order, omsvc.GetOpenOrders("ETHUSD")[0]);
            SocketTestBase.Closed(webTuple.Item2);
            Assert.Throws<ApplicationException>(() => omsvc.GetOpenOrders("ETHUSD"));
        }
    }
}
