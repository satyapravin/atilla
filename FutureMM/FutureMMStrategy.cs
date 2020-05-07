using Exchange;
using Hedger;
using Quoter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FutureMM
{
    class FutureMMStrategy
    {
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(FutureMMStrategy));
        BitmexExchange exchange;
        string spot;
        string future;
        volatile bool stop = false;
        QuotingService spotQuoter;
        HedgingService futureHedger;
        Thread backGroundThread;
        object positionLocker = new object();

        public FutureMMStrategy(BitmexExchange exch, string spot, string fut)
        {
            exchange = exch;
            this.spot = spot;
            this.future = fut;
        }

        private void OnPositionUpdate(PositionUpdate update)
        {
            log.Info("OnPositionUpdate");

            lock (positionLocker)
            {
                if (update.symbol == spot)
                {
                    log.Info("Spot position update");                    
                    futureHedger.Target(-update.currentQty);
                }                
            }
        }

        public void Start()
        {
            log.Info("Strategy started");
            spotQuoter = new QuotingService(exchange, spot, 0);
            futureHedger = new HedgingService(exchange, future, future);

            exchange.PositionSubscribe(spot, OnPositionUpdate);
            futureHedger.Start();
            Thread.Sleep(1000);
            exchange.Start();
            Thread.Sleep(5000);
            spotQuoter.Start();
            Thread.Sleep(2000);
            backGroundThread = new Thread(new ThreadStart(Run));
            backGroundThread.Start();
        }

        private void Run()
        {
            log.Info("Strategy running");
            while (!stop)
            {
                try
                {
                    decimal baseQuantity = 100;
                    var spotP = exchange.MarketDataSystem.GetBidAsk(spot);
                    var futureP = exchange.MarketDataSystem.GetBidAsk(future);
                    log.Info(string.Format("{0} bidAsk = {1}/{2}, {3} bidAsk={4}/{5}", spot, spotP.Item1, spotP.Item2, future, futureP.Item1, futureP.Item2));

                    bool currentBasisPositive = futureP.Item2 > spotP.Item1 + spotP.Item1 * 0.001m;
                    bool currentBasisNegative = futureP.Item1 < spotP.Item2 - spotP.Item2 * 0.001m;

                    var spotPos = exchange.PositionSystem.GetPosition(spot);
                    var futPos = exchange.PositionSystem.GetPosition(future);

                    var currFutPos = futPos == null ? 0m : futPos.CurrentQty;
                    var currSpotPos = spotPos == null ? 0m : spotPos.CurrentQty;

                    if ((currFutPos == 0 && currSpotPos == 0))
                    {
                        if (currentBasisPositive)
                        {
                            spotQuoter.SetQuote(baseQuantity, 0, 0, 0, spotP.Item1, spotP.Item2);
                        }
                        else if (currentBasisNegative)
                        {
                            spotQuoter.SetQuote(0, baseQuantity, 0, 0, spotP.Item1, spotP.Item2);
                        }
                    }
                    else if ((futPos == null && spotPos != null) || (futPos != null && spotPos == null)
                     || Math.Abs(spotPos.CurrentQty) != Math.Abs(futPos.CurrentQty))
                    {
                        log.Info(string.Format("Future {0}, Spot {1} not balanced", 
                            futPos != null ? futPos.CurrentQty : 0, 
                            spotPos != null ? spotPos.CurrentQty : 0));
                        Thread.Sleep(2000);
                        continue;
                    }
                    else
                    {
                        bool positiveSpotPosition = spotPos.CurrentQty > 0;
                        var posBasis = (futPos.AvgEntryPrice.Value - spotPos.AvgEntryPrice.Value) / futPos.AvgEntryPrice.Value;
                        
                        if (positiveSpotPosition)
                        {
                            var spread = 0m;

                            if (currentBasisPositive)
                            {
                                var currBasis = (futureP.Item1 - spotP.Item2) / spotP.Item2;
                                spread = currBasis - posBasis + 0.001m;
                            }
                            
                            spotQuoter.SetQuote(0, Math.Abs(spotPos.CurrentQty), 0, spread, spotP.Item1, spotP.Item2);
                        }
                        else
                        {
                            var spread = 0m;

                            if (currentBasisNegative)
                            {
                                var currBasis = (futureP.Item2 - spotP.Item1) / spotP.Item1;
                                spread = posBasis - currBasis + 0.001m;
                            }
                            
                            spotQuoter.SetQuote(Math.Abs(spotPos.CurrentQty), 0, spread, 0, spotP.Item1, spotP.Item2);
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Fatal("Strategy thread excepted", e);
                    stop = true;
                }

                Thread.Sleep(2000);
            }
        }

        public void Join()
        {
            backGroundThread.Join();
        }

        public void Stop()
        {
            log.Info("Strategy stopping");
            stop = true;
            Thread.Sleep(2000);
            exchange.Stop();
            log.Info("Strategy stopped");
        }
    }
}
