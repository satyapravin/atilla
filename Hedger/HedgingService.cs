using Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hedger
{
    public class HedgingService
    {
        private BitmexExchange exchange;
        private string symbol;
        private string future;
        private decimal quantity;
        private volatile bool stop = false;
        private Thread backGroundThread;
        private volatile bool isSet = false;
        private object locker = new object();
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(HedgingService));

        public HedgingService(BitmexExchange exch, string sym, string fut)
        {
            exchange = exch;
            symbol = sym;
            future = fut;
            quantity = 0;
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

        public void Target(decimal qty)
        {
            lock (locker)
            {
                quantity = qty;
                isSet = true;
                log.Info(string.Format("target {0} set for {1},{2}", quantity, symbol, future));
            }
        }

        public void Add(decimal qty)
        {
            lock (locker)
            {
                if (quantity != 0)
                {
                    quantity += qty;
                    isSet = true;
                    log.Info(string.Format("Add {0}, new target {1} for {2},{3}", qty, quantity, symbol, future));
                }
            }
        }

        private void OnStart()
        {
            while(!stop)
            {
                if (isSet)
                {
                    try
                    {
                        decimal tradeQty = 0;
                        lock (locker)
                        {
                            tradeQty = quantity;
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

                        if (tradeQty == 0)
                        {
                            log.Info("Target is 0 closing all positions");
                            var currSpotQty = 0m;
                            var currFutQty = 0m;
                            if (spotPosition != null)
                                currSpotQty = spotPosition.CurrentQty;

                            if (futurePosition != null)
                                currFutQty = futurePosition.CurrentQty;

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

                            Thread.Sleep(2000);
                            continue;
                        }

                        if (spotPosition != null)
                        {
                            tradeQty -= spotPosition.CurrentQty;
                        }
                        else
                        {
                            log.Info(string.Format("Hedger cannot find position for {0}", symbol));
                        }

                        if (futurePosition != null)
                        {
                            tradeQty -= futurePosition.CurrentQty;
                        }

                        if (tradeQty != 0)
                        {
                            string tradeSymbol;
                            var spotP = exchange.MarketDataSystem.GetBidAsk(symbol);
                            var futP = exchange.MarketDataSystem.GetBidAsk(future);

                            if (spotP.Item1 < futP.Item1)
                            {
                                if (tradeQty > 0)
                                    tradeSymbol = symbol;
                                else
                                    tradeSymbol = future;
                            }
                            else
                            {
                                if (tradeQty > 0)
                                    tradeSymbol = future;
                                else
                                    tradeSymbol = symbol;
                            }

                            log.Info(string.Format("New order of {0} for {1}", tradeQty, tradeSymbol));
                            newMarketOrder(tradeSymbol, tradeQty);
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
}
