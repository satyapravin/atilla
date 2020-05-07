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
        string ethFutureInstr;
        string btcInstr;
        string btcFutureInstr;
        volatile bool stop = false;
        QuotingService quoter;
        HedgingService ethHedger;
        HedgingService btcHedger;
        Thread backGroundThread;
        decimal oldQty = 0;
        Rebalancer rebalancer;
        object positionLocker = new object();
        ConcurrentDictionary<DateTime, Tuple<decimal, decimal, decimal>> rebalanceTimes = new ConcurrentDictionary<DateTime, Tuple<decimal, decimal, decimal>>();

        public CryptoQuantoCorrStrategy(BitmexExchange exch, string ethbtc, string eth, 
            string ethfut, string btc, string btcfut)
        {
            exchange = exch;
            ethbtcInstr = ethbtc;
            ethInstr = eth;
            btcInstr = btc;
            ethFutureInstr = ethfut;
            btcFutureInstr = btcfut;
        }

        private void OnPositionUpdate(PositionUpdate update)
        {
            log.Info("OnPositionUpdate");

            lock (positionLocker)
            {
                if (update.symbol == ethbtcInstr && oldQty != update.currentQty)
                {
                    log.Info("ETHBTC Quantity changed");
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
            ethHedger = new HedgingService(exchange, ethInstr, ethFutureInstr);
            btcHedger = new HedgingService(exchange, btcInstr, btcFutureInstr);

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
            rebalancer = new Rebalancer(3600000, exchange, ethInstr, btcInstr, ethFutureInstr, btcFutureInstr, ethHedger, btcHedger, rebalanceTimes);
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
                    decimal baseQuantity = 5;
                    var ethP = exchange.MarketDataSystem.GetBidAsk(ethInstr);
                    var ethFP = exchange.MarketDataSystem.GetBidAsk(ethFutureInstr);
                    var xbtP = exchange.MarketDataSystem.GetBidAsk(btcInstr);
                    var xbtFP = exchange.MarketDataSystem.GetBidAsk(btcFutureInstr);
                    log.Info(string.Format("{0} bidAsk = {1}/{2}, {3} bidAsk={4}/{5}", ethInstr, ethP.Item1, ethP.Item2, btcInstr, xbtP.Item1, xbtP.Item2));
                    log.Info(string.Format("{0} bidAsk = {1}/{2}, {3} bidAsk={4}/{5}", ethFutureInstr, ethFP.Item1, ethFP.Item2, btcFutureInstr, xbtFP.Item1, xbtFP.Item2));

                    var ethBid = ethFP.Item1 < ethP.Item1 ? ethFP.Item1 : ethP.Item1;
                    var ethAsk = ethFP.Item2 > ethP.Item2 ? ethFP.Item2 : ethP.Item2;
                    var xbtBid = xbtFP.Item1 < xbtP.Item1 ? xbtFP.Item1 : xbtP.Item1;
                    var xbtAsk = xbtFP.Item2 > xbtP.Item2 ? xbtFP.Item2 : xbtP.Item2;

                    var ethbtcAsk = ethBid / xbtAsk;
                    var ethbtcBid = ethBid / xbtBid;

                    decimal spread = 0.0016m * 20;
                    var pos = exchange.PositionSystem.GetPosition(ethbtcInstr);
                    decimal posQty = 0;

                    if (pos != null)
                    {
                        posQty = pos.CurrentQty;
                    }

                    if (posQty == 0)
                    {
                        quoter.SetQuote(baseQuantity, baseQuantity, spread, 0, ethbtcBid, ethbtcAsk);
                    }
                    else if (posQty > 0)
                    {
                        quoter.SetQuote(Math.Max(0, baseQuantity - Math.Abs(posQty)), baseQuantity, spread, 0, ethbtcBid, ethbtcAsk);
                    }
                    else
                    {
                        quoter.SetQuote(baseQuantity, Math.Max(0, baseQuantity - Math.Abs(posQty)), spread, 0, ethbtcBid, ethbtcAsk);

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
