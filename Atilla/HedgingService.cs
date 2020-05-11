using Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atilla
{
    public abstract class HedgingService
    {
        private IExchange exchange;
        private string symbol;
        private string future;
        private decimal notional;
        private volatile bool stop = false;
        private Thread backGroundThread;
        private volatile bool isSet = false;
        private object locker = new object();
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(HedgingService));
        bool basePositivePosition = false;

        public HedgingService(IExchange exch, string sym, string fut, bool posBase)
        {
            exchange = exch;
            symbol = sym;
            future = fut;
            notional = 0;
            basePositivePosition = posBase;
        }

        public void Start()
        {
            log.Info(string.Format("{0},{1} hedging service starting", symbol, future));
            backGroundThread = new Thread(new ThreadStart(OnStart));
            backGroundThread.Start();
        }

        public void Stop()
        {
            stop = true;
            log.Info(string.Format("{0},{1} hedging service stopped", symbol, future));
        }

        public void Target(decimal not)
        {
            lock (locker)
            {
                notional = not;
                isSet = true;
                log.Info(string.Format("target {0} set for {1}/{2}", not, symbol, future));
            }
        }

        public void Rebalance()
        {
            lock (locker)
            {
                isSet = true;
                log.Info(string.Format("Rebalance instruction for {0}/{1}", symbol, future));
            }
        }

        protected abstract decimal getNotional(decimal quantity, Tuple<decimal, decimal> bidAsk);
        protected abstract decimal getQuantity(decimal notional, Tuple<decimal, decimal> bidAsk);
        
        private void OnStart()
        {
            while(!stop)
            {
                if (isSet)
                {
                    try
                    {
                        decimal tradeNotional = 0;
                        lock (locker)
                        {
                            tradeNotional = notional;
                        }

                        var spotPosition = exchange.PositionSystem.GetPosition(symbol);
                        var futurePosition = exchange.PositionSystem.GetPosition(future);

                        if (spotPosition == null)
                        {
                            log.Error(string.Format("Spot position not found, querying exchange for {0}", symbol));
                            spotPosition = exchange.PositionSystem.QueryPositionFromExchange(symbol);
                        }

                        if (futurePosition == null)
                        {
                            log.Error(string.Format("Future position not found, querying exchange for {0}", future));
                            futurePosition = exchange.PositionSystem.QueryPositionFromExchange(future);
                        }

                        var currSpotQty = 0m;
                        var currFutQty = 0m;
                        if (spotPosition != null)
                            currSpotQty = spotPosition.CurrentQty;

                        if (futurePosition != null)
                            currFutQty = futurePosition.CurrentQty;

                        if (tradeNotional == 0)
                        {
                            log.Info("Target is 0 closing all positions");
                            if (currSpotQty != 0)
                            {
                                log.Info(string.Format("Closing {0} of {1}", currSpotQty, symbol));
                                newMarketOrder(symbol, -currSpotQty);
                            }

                            if (currFutQty != 0)
                            {
                                log.Info(string.Format("Closing {0} of {1}", currFutQty, future));
                                newMarketOrder(future, -currFutQty);
                            }

                            isSet = false;
                            Thread.Sleep(2000);
                            continue;
                        }

                        log.Info(string.Format("Current quantity {0}={1}, {2}={3}", symbol, currSpotQty, future, currFutQty));
                        var spotP = exchange.MarketDataSystem.GetBidAsk(symbol);
                        var futP = exchange.MarketDataSystem.GetBidAsk(future);

                        tradeNotional -= getNotional(currSpotQty, spotP);
                        tradeNotional -= getNotional(futurePosition.CurrentQty, futP);
                        log.Info(string.Format("Trade notional={0} for {1}/{2}", tradeNotional, symbol, future));

                        if (tradeNotional != 0)
                        {
                            string tradeSymbol;

                            Tuple<decimal, decimal> refPrice = null;

                            if (spotP.Item1 < futP.Item1)
                            {
                                if (tradeNotional > 0)
                                {
                                    if (basePositivePosition)
                                    {
                                        tradeSymbol = symbol;
                                        refPrice = spotP;
                                    }
                                    else if (Math.Abs(currSpotQty) < Math.Abs(currFutQty))
                                    {
                                        tradeSymbol = symbol;
                                        refPrice = spotP;
                                    }
                                    else
                                    {
                                        tradeSymbol = future;
                                        refPrice = futP;
                                    }
                                }
                                else
                                {
                                    if (!basePositivePosition)
                                    {
                                        tradeSymbol = future;
                                        refPrice = futP;
                                    }
                                    else if (Math.Abs(currFutQty) < Math.Abs(currSpotQty))
                                    {
                                        tradeSymbol = future;
                                        refPrice = futP;
                                    }
                                    else
                                    {
                                        tradeSymbol = symbol;
                                        refPrice = spotP;
                                    }
                                }
                            }
                            else
                            {
                                if (tradeNotional > 0)
                                {
                                    if (basePositivePosition)
                                    {
                                        tradeSymbol = future;
                                        refPrice = futP;
                                    }
                                    else if (Math.Abs(currFutQty) < Math.Abs(currSpotQty))
                                    {
                                        tradeSymbol = future;
                                        refPrice = futP;
                                    }
                                    else
                                    {
                                        tradeSymbol = symbol;
                                        refPrice = spotP;
                                    }
                                }
                                else
                                {
                                    if (!basePositivePosition)
                                    {
                                        tradeSymbol = symbol;
                                        refPrice = spotP;
                                    }
                                    if (Math.Abs(currSpotQty) < Math.Abs(currFutQty))
                                    {
                                        tradeSymbol = symbol;
                                        refPrice = spotP;
                                    }
                                    else
                                    {
                                        tradeSymbol = future;
                                        refPrice = futP;
                                    }
                                }
                            }

                            log.Info(string.Format("Selected {0} for quantities {1}/{2} and prices {3}/{4}", 
                                tradeSymbol, currSpotQty, currFutQty, spotP.Item1, futP.Item1));
                            var tradeQty = getQuantity(tradeNotional, refPrice);

                            if (tradeQty != 0)
                            {
                                log.Info(string.Format("New order of {0} for {1}", tradeQty, tradeSymbol));
                                newMarketOrder(tradeSymbol, tradeQty);
                            }
                            else
                            {
                                log.Info(string.Format("No need to rebalance now for {0}", tradeSymbol));
                            }

                            isSet = false;
                        }
                    }
                    catch(Exception e)
                    {
                        log.Fatal(string.Format("HedgingService for {0} crashed",  symbol), e);
                    }
                }
                else
                {
                    log.Info("target not set");
                }

                Thread.Sleep(2000);
            }
        }

        private void newMarketOrder(string tradeSymbol, decimal tradeQty)
        {
            log.Info(string.Format("Sending new order of {0} for {1}", tradeQty, tradeSymbol));
            exchange.OrderSystem.NewOrder(
                new OrderRequest()
                {
                    orderType = OrderType.NEW_MARKET_ORDER,
                    quantity = Math.Abs(tradeQty),
                    symbol = tradeSymbol,
                    side = tradeQty > 0 ? OrderSide.BUY : OrderSide.SELL
                });

        }
    }

    public class ETHHedgingService : HedgingService
    {
        public ETHHedgingService(IExchange exch, string symbol, string future):base(exch, symbol, future, true)
        { }
            
        protected override decimal getNotional(decimal quantity, Tuple<decimal, decimal> bidAsk)
        {
            decimal retval = 0.000001m;

            if (quantity > 0)
            {
                return retval * quantity * bidAsk.Item2;
            }
            else
            {
                return retval * quantity * bidAsk.Item1;
            }
        }

        protected override decimal getQuantity(decimal notional, Tuple<decimal, decimal> bidAsk)
        {
            if (notional > 0)
            {
                return Math.Round((notional / 0.000001m) / bidAsk.Item2);
            }
            else
            {
                return Math.Round((notional / 0.000001m) / bidAsk.Item1);
            }
        }
    }

    public class BTCHedgingService : HedgingService
    {
        public BTCHedgingService(IExchange exch, string symbol, string future) : base(exch, symbol, future, false)
        { }

        protected override decimal getNotional(decimal quantity, Tuple<decimal, decimal> bidAsk)
        {
            decimal retval = quantity;

            if (quantity > 0)
            {
                retval /= bidAsk.Item2;
            }
            else
            {
                retval /= bidAsk.Item1;
            }

            return retval;
        }

        protected override decimal getQuantity(decimal notional, Tuple<decimal, decimal> bidAsk)
        {
            if (notional > 0)
            {
                return Math.Round(notional * bidAsk.Item2);
            }
            else
            {
                return Math.Round(notional * bidAsk.Item1);
            }

        }
    }
}
