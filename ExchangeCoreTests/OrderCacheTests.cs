using ExchangeCore;
using NUnit.Framework;
using TestsBase;
using System;
using BitmexCore.Dtos;

namespace ExchangeCoreTests
{
    public class OrderCacheTests
    {
        OrderDto GetOrder(string id, DateTimeOffset offset, string status, decimal qty, decimal cumQty)
        {
            var dto = OrderTestBase.GetDummyOrder();
            dto.OrderId = id;
            dto.OrdStatus = status;
            dto.TransactTime = offset;
            dto.OrderQty = qty;
            dto.CumQty = cumQty;
            return dto;
        }

        [Test]
        public void TestOrderCache()
        {
            var cache = new OrderCache();
            Assert.IsTrue(cache.GetOpenOrders().Count == 0);
            var offset = new DateTimeOffset(DateTime.UtcNow);
            cache.Reconcile(GetOrder("123", offset, "PartiallyFilled", 100, 50));
            Assert.IsTrue(cache.GetOpenOrders().Count == 1);
            Assert.IsTrue(cache.GetOpenOrders()[0].CumQty == 50);
            offset = offset.AddMilliseconds(1);
            cache.Reconcile(GetOrder("123", offset, "PartiallyFilled", 100, 51));
            Assert.IsTrue(cache.GetOpenOrders()[0].CumQty == 51);
            offset = offset.AddMilliseconds(-1);
            cache.Reconcile(GetOrder("123", offset, "PartiallyFilled", 100, 40));
            Assert.IsTrue(cache.GetOpenOrders()[0].CumQty == 51);
            offset = offset.AddMilliseconds(5);
            cache.Reconcile(GetOrder("123", offset, "Filled", 100, 100));
            Assert.IsTrue(cache.GetOpenOrders().Count == 0);
        }
    }
}