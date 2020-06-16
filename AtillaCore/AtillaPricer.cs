using System;
using System.Collections.Generic;
using System.Text;

namespace AtillaCore
{
    using BidAsk = Tuple<decimal, decimal>;
    public static class AtillaPricer
    {
        private static readonly decimal ETHMULTIPLIER = 0.000001m;

        public static decimal GetBTCQuantityFromETHBTCQuantity(decimal ethBtcQty, BidAsk ethBidAsk)
        {
            if (ethBtcQty > 0)
            {
                return Math.Round(ethBtcQty * ethBidAsk.Item2);
            }
            else
            {
                return Math.Round(ethBtcQty * ethBidAsk.Item1);
            }
        }

        public static decimal GetETHQuantityFromETHBTCQuantity(decimal ethBtcQty, BidAsk btcBidAsk)
        {
            if (ethBtcQty > 0)
            {
                return Math.Round(ethBtcQty / btcBidAsk.Item2 / ETHMULTIPLIER);
            }
            else
            {
                return Math.Round(ethBtcQty / btcBidAsk.Item1 / ETHMULTIPLIER);
            }
        }
    
        public static BidAsk ComputeETHBTCBidAsk(BidAsk eth, BidAsk ethFuture, BidAsk xbt, BidAsk xbtFuture)
        {
            if (!Validate(eth))
            {
                throw new ArgumentException("Non positive values for bidAsk for ETH");
            }

            if (!Validate(ethFuture))
            {
                throw new ArgumentException("Non positive values for bidAsk for ETHFuture");
            }

            if (!Validate(xbt))
            {
                throw new ArgumentException("Non positive values for bidAsk for XBT");
            }

            if (!Validate(xbtFuture))
            {
                throw new ArgumentException("Non positive values for bidAsk for XBTFuture");
            }

            var ethBid = ethFuture.Item1 < eth.Item1 ? ethFuture.Item1 : eth.Item1;
            var xbtAsk = xbtFuture.Item2 > xbt.Item2 ? xbtFuture.Item2 : xbt.Item2;

            var ethbtc = ethBid / xbtAsk;
            return new Tuple<decimal, decimal>(ethbtc, ethbtc);
        }

        private static bool Validate(BidAsk bidAsk)
        {
            if (bidAsk.Item1 <= 0 || bidAsk.Item2 <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
