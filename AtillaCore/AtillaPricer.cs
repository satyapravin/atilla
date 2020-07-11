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
    
        public static BidAsk ComputeETHBTCBidAsk(BidAsk eth, BidAsk xbt)
        {
            if (!Validate(eth))
            {
                throw new ArgumentException("Non positive values for bidAsk for ETH");
            }

            if (!Validate(xbt))
            {
                throw new ArgumentException("Non positive values for bidAsk for XBT");
            }

            var ethBid = eth.Item1;
            var xbtAsk = xbt.Item2;

            var ethbtcAsk = ethBid / xbtAsk;

            var ethAsk = eth.Item2;
            var xbtBid = xbt.Item1;
            var ethbtcBid = ethAsk / xbtBid;

            return new Tuple<decimal, decimal>(ethbtcBid, ethbtcAsk);
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
