using BitmexCore.Dtos;
using System;
using NUnit.Framework;

namespace TestsBase
{
    public class OrderTestBase
    {
        public static OrderDto GetDummyOrder()
        {
            OrderDto testOrderDtoOne = new OrderDto
            {
                Account = 123,
                AvgPx = 100.1m,
                ClOrdId = string.Empty,
                ClOrdLinkId = string.Empty,
                CumQty = 100,
                Currency = "ETH",
                DisplayQty = 100,
                ExDestination = string.Empty,
                ExecInst = "ParticipateDoNotInitiate",
                LeavesQty = 100,
                MultiLegReportingType = string.Empty,
                OrderId = "order1",
                OrderQty = 200,
                OrdRejReason = string.Empty,
                OrdStatus = "PartiallyFilled",
                OrdType = "Limit",
                PegPriceType = string.Empty,
                Price = 200.15m,
                SettlCurrency = "XBT",
                Side = "Buy",
                Symbol = "ETHUSD",
                Timestamp = new DateTimeOffset(DateTime.UtcNow),
                TransactTime = new DateTimeOffset(DateTime.UtcNow),
                WorkingIndicator = true
            };

            return testOrderDtoOne;
        }

        public static void Compare(OrderDto expected, OrderDto actual)
        {
            Assert.AreEqual(expected.Account, actual.Account);
            Assert.AreEqual(expected.AvgPx, actual.AvgPx);
            Assert.AreEqual(expected.ClOrdId, actual.ClOrdId);
            Assert.AreEqual(expected.CumQty, actual.CumQty);
            Assert.AreEqual(expected.Currency, actual.Currency);
            Assert.AreEqual(expected.DisplayQty, actual.DisplayQty);
            Assert.AreEqual(expected.ExecInst, actual.ExecInst);
            Assert.AreEqual(expected.LeavesQty, actual.LeavesQty);
            Assert.AreEqual(expected.OrderId, actual.OrderId);
            Assert.AreEqual(expected.OrderQty, actual.OrderQty);
            Assert.AreEqual(expected.OrdRejReason, actual.OrdRejReason);
            Assert.AreEqual(expected.OrdStatus, actual.OrdStatus);
            Assert.AreEqual(expected.OrdType, actual.OrdType);
            Assert.AreEqual(expected.Price, actual.Price);
            Assert.AreEqual(expected.Side, actual.Side);
            Assert.AreEqual(expected.Symbol, actual.Symbol);
            Assert.AreEqual(expected.Timestamp, actual.Timestamp);
            Assert.AreEqual(expected.TransactTime, actual.TransactTime);
            Assert.AreEqual(expected.WorkingIndicator, actual.WorkingIndicator);
        }
    }
}
