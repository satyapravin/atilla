using Exchange;
using Hedger;
using Quoter;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Atilla
{
    class CryptoQuantoCorrStrategy
    {
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(CryptoQuantoCorrStrategy));
        BitmexExchange exchange;
        string ethbtcInstr;
        string ethInstr;
        string btcInstr;
        volatile bool stop = false;
        QuotingService quoter;
        HedgingService ethHedger;
        HedgingService btcHedger;
        Thread backGroundThread;
        decimal oldQty = 0;
        Rebalancer rebalancer;
        object positionLocker = new object();
        ConcurrentDictionary<DateTime, Tuple<decimal, decimal, decimal>> rebalanceTimes = new ConcurrentDictionary<DateTime, Tuple<decimal, decimal, decimal>>();

        public CryptoQuantoCorrStrategy(BitmexExchange exch, string ethbtc, string eth, string btc)
        {
            exchange = exch;
            ethbtcInstr = ethbtc;
            ethInstr = eth;
            btcInstr = btc;
        }

        private void OnPositionUpdate(PositionUpdate update)
        {
            log.Info("OnPositionUpdate");

            lock (positionLocker)
            {
                if (update.symbol == ethbtcInstr && oldQty != update.currentQty)
                {
                    log.Info("Quantity changed");
                    var ethP = exchange.MarketDataSystem.GetBidAsk(ethInstr);
                    var btcP = exchange.MarketDataSystem.GetBidAsk(btcInstr);
                    decimal ethQty = 0;
                    decimal btcQty = 0;

                    Tuple<decimal, decimal, decimal> rebalanceTime = null;

                    if (update.currentQty > 0)
                    {
                        ethQty = -Math.Round(update.currentQty / btcP.Item2 / 0.000001m);
                        btcQty = Math.Round(update.currentQty * ethP.Item1);
                        rebalanceTimes.Clear();
                        log.Info("Positive position, rebalancing cleared");
                    }
                    else if (update.currentQty < 0)
                    {
                        ethQty = -Math.Round(update.currentQty / btcP.Item1 / 0.000001m);
                        btcQty = Math.Round(update.currentQty * ethP.Item2);
                        rebalanceTime = new Tuple<decimal, decimal, decimal>(Math.Max(update.currentQty, update.currentQty - oldQty), 
                                                                             ethQty, btcQty);
                        log.Info("Negative position, rebalance created");
                    }


                    if (rebalanceTime != null)
                    {
                        rebalanceTimes.AddOrUpdate(DateTime.UtcNow.AddHours(1), rebalanceTime, (k, v) => rebalanceTime);
                        log.Info("Rebalance added");
                    }

                    oldQty = update.currentQty;
                    log.Info(string.Format("About to hedge, ETH={0}, BTC={1}", ethQty, btcQty));
                    ethHedger.Target(ethQty);
                    btcHedger.Target(btcQty);
                }
            }
        }

        public void Start()
        {
            log.Info("Strategy started");
            quoter = new QuotingService(exchange, ethbtcInstr, 5);
            ethHedger = new HedgingService(exchange, ethInstr);
            btcHedger = new HedgingService(exchange, btcInstr);

            exchange.PositionSubscribe(ethbtcInstr, OnPositionUpdate);
            exchange.Start();
            Thread.Sleep(2000);
            Thread.Sleep(5000);
            ethHedger.Start();
            Thread.Sleep(1000);
            btcHedger.Start();
            Thread.Sleep(1000);
            quoter.Start();
            Thread.Sleep(2000);
            rebalancer = new Rebalancer(3600000, exchange, ethInstr, btcInstr, ethHedger, btcHedger, rebalanceTimes);
            backGroundThread = new Thread(new ThreadStart(Run));
            backGroundThread.Start();
            rebalancer.Start();
        }

        private void Run()
        {
            log.Info("Strategy running");
            while (!stop)
            {
                try
                {
                    decimal baseQuantity = 4;
                    var ethP = exchange.MarketDataSystem.GetBidAsk(ethInstr);
                    var xbtP = exchange.MarketDataSystem.GetBidAsk(btcInstr);
                    var ethbtcBid = ethP.Item1 / xbtP.Item2;
                    var ethbtcAsk = ethP.Item2 / xbtP.Item1;
                    decimal spread = 0.0015m;
                    var pos = exchange.PositionSystem.GetPosition(ethbtcInstr);

                    if (pos == null)
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    if (pos.CurrentQty == 0)
                    {
                        quoter.SetQuote(baseQuantity, baseQuantity, spread, spread, ethbtcBid, ethbtcAsk);
                    }
                    else if (pos.CurrentQty > 0)
                    {
                        quoter.SetQuote(Math.Max(0, baseQuantity - pos.CurrentQty), baseQuantity, spread, spread * 2, ethbtcBid, ethbtcAsk);
                    }
                    else
                    {
                        quoter.SetQuote(baseQuantity, Math.Max(0, baseQuantity - Math.Abs(pos.CurrentQty)), spread * 2, spread, ethbtcBid, ethbtcAsk);

                    }
                }
                catch(Exception e)
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
