using System;
using System.Collections.Generic;
using System.Text;

namespace AtillaCore
{
    using BidAsk = Tuple<decimal, decimal>;
    public static class AtillaPricer
    {
        private static readonly decimal ETHMULTIPLIER = 0.000001m;

        private static decimal ETHNotionalToBTC(decimal ethNotional, BidAsk ethBidAsk)
        {
            if (ethNotional > 0)
            {
                return ethNotional * ethBidAsk.Item2;
            }
            else
            {
                return ethNotional * ethBidAsk.Item1;
            }
        }

        public static decimal GetETHNotionalInETH(decimal ethQuantity)
        {
            return ETHMULTIPLIER * ethQuantity;
        }

        public static decimal GetETHNotionalInBTC(decimal ethQuantity, BidAsk ethBidAsk)
        {
            if (ethQuantity > 0)
            {
                return ethQuantity * ethBidAsk.Item2;
            }
            else
            {
                return ethQuantity * ethBidAsk.Item1;
            }
        }

        public static decimal GetETHQuantityFromETHNotional(decimal ethNotional)
        {
            return Math.Round(ethNotional / ETHMULTIPLIER);
        }
    
        public static decimal GetBTCNotionalInBTC(decimal btcQuantity, BidAsk btcBidAsk)
        {
            decimal retval = btcQuantity;

            if (btcQuantity > 0)
            {
                retval /= btcBidAsk.Item2;
            }
            else
            {
                retval /= btcBidAsk.Item1;
            }

            return retval;
        }

        public static decimal GetBTCQuantityFromBTCNotional(decimal btcNotional, BidAsk btcBidAsk)
        {
            if (btcNotional > 0)
            {
                return Math.Round(btcNotional * btcBidAsk.Item2);
            }
            else
            {
                return Math.Round(btcNotional * btcBidAsk.Item1);
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
