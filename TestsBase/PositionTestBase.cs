using System;
using System.Collections.Generic;
using System.Text;
using BitmexCore.Dtos;
using NUnit.Framework;

namespace TestsBase
{
    public class PositionTestBase
    {
        public static PositionDto GetDummyPosition()
        {
            var dto = new PositionDto()
            {
                Account = 123,
                Symbol = "ETHUSD",
                Currency = "ETH",
                Underlying = "ETH",
                QuoteCurrency = "ETH",
                Commission = 0,
                InitMarginReq = 0,
                MaintMarginReq = 0,
                RiskLimit = 100,
                Leverage = 5,
                CrossMargin = true,
                DeleveragePercentile = 0.9m,
                RebalancedPnl = 0,
                PrevRealisedPnl = 10.2m,
                PrevUnrealisedPnl = 10.2m,
                PrevClosePrice = 200.1m,
                OpeningTimestamp = new DateTimeOffset(DateTime.UtcNow),
                OpeningQty = 100,
                OpeningCost = 2015.5m,
                OpeningComm = 5.5m,
                OpenOrderBuyQty = 100,
                OpenOrderBuyCost = 2015.5m,
                OpenOrderBuyPremium = 10.2m,
                OpenOrderSellQty = 0,
                OpenOrderSellCost = 0,
                OpenOrderSellPremium = 0,
                ExecBuyQty = 100,
                ExecBuyCost = 2015.5m,
                ExecSellQty = 0,
                ExecSellCost = 0,
                ExecQty = 100,
                ExecCost = 2015.5m,
                ExecComm = 10.2m,
                CurrentTimestamp = new DateTimeOffset(DateTime.UtcNow),
                CurrentQty = 100,
                CurrentCost = 2015.5m,
                CurrentComm = 10.2m,
                RealisedCost = 10.2m,
                UnrealisedCost = 2012.3m,
                GrossOpenCost = 2015.5m,
                GrossOpenPremium = 10.2m,
                GrossExecCost = 2015.5m,
                IsOpen = true,
                MarkPrice = 210,
                MarkValue = 210,
                RiskValue = 2000,
                HomeNotional = 10,
                ForeignNotional = 2015,
                PosState = "Open",
                PosCost = 0,
                PosCost2 = 0,
                PosCross = 0,
                PosInit = 0,
                PosComm = 0,
                PosLoss = 0,
                PosMargin = 0,
                PosMaint = 0,
                PosAllowance = 0,
                TaxableMargin = 0,
                InitMargin = 0,
                MaintMargin = 0,
                SessionMargin = 0,
                TargetExcessMargin = 0,
                VarMargin = 0,
                RealisedGrossPnl = 0,
                RealisedTax = 0,
                RealisedPnl = 0,
                UnrealisedGrossPnl = 100.2m,
                LongBankrupt = 0,
                ShortBankrupt = 0,
                TaxBase = 0,
                IndicativeTaxRate = 0,
                IndicativeTax = 0,
                UnrealisedTax = 0,
                UnrealisedPnl = 20.1m,
                UnrealisedPnlPcnt = 1.1m,
                UnrealisedRoePcnt = 0,
                AvgCostPrice = 0,
                AvgEntryPrice = 0,
                BreakEvenPrice = 201,
                MarginCallPrice = 105,
                LiquidationPrice = 101,
                BankruptPrice = 100,
                Timestamp = new DateTimeOffset(DateTime.UtcNow),
                LastPrice = 210.1m,
                LastValue = 2101
            };

            return dto;
        }


        public static void Compare(PositionDto expected, PositionDto actual)
        {
            foreach (var prop in typeof(PositionDto).GetProperties())
            {
                Assert.AreEqual(prop.GetValue(expected), prop.GetValue(actual));
            }
        }
    }
}
