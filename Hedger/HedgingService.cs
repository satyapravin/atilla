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
        private decimal quantity;
        private volatile bool stop = false;
        private Thread backGroundThread;
        private volatile bool isSet = false;
        private object locker = new object();
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(HedgingService));

        public HedgingService(BitmexExchange exch, string sym)
        {
            exchange = exch;
            symbol = sym;
            quantity = 0;
        }

        public void Start()
        {
            log.Info(string.Format("{0} hedging service starting", symbol));
            backGroundThread = new Thread(new ThreadStart(OnStart));
            backGroundThread.Start();
        }

        public void Stop()
        {
            stop = true;
            log.Info(string.Format("{0} hedging service stopped", symbol));
        }

        public void Target(decimal qty)
        {
            lock (locker)
            {
                quantity = qty;
                isSet = true;
                log.Info(string.Format("target {0} set for {1}", quantity, symbol));
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
                    log.Info(string.Format("Add {0}, new target {1} for {2}", qty, quantity, symbol));
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

                        var position = exchange.PositionSystem.GetPosition(symbol);

                        if (position != null)
                        {
                            tradeQty -= position.CurrentQty;
                        }

                        if (tradeQty != 0)
                        {
                            exchange.OrderSystem.NewOrder(
                                new OrderRequest()
                                {
                                    orderType = OrderType.NEW_MARKET_ORDER,
                                    quantity = Math.Abs(tradeQty),
                                    symbol = symbol,
                                    side = tradeQty > 0 ? OrderSide.BUY : OrderSide.SELL
                                });
                        }
                    }
                    catch(Exception e)
                    {
                        log.Fatal(string.Format("HedgingService for {0} died",  symbol), e);
                        stop = true;
                    }
                }
                else
                {
                    log.Info("target not set");
                }

                Thread.Sleep(2000);
            }
        }
    }
}
