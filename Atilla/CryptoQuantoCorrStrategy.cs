using Exchange;
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
    public class CryptoQuantoCorrStrategy
    {
        #region private members
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(CryptoQuantoCorrStrategy));
        IExchange exchange;
        string ethbtcInstr;
        string ethInstr;
        string ethFutureInstr;
        string btcInstr;
        string btcFutureInstr;
        volatile bool stop = false;
        IQuotingServiceFactory qFactory;
        IQuotingService quoter;
        HedgingService ethHedger;
        HedgingService btcHedger;
        Thread backGroundThread;
        decimal oldQty = 0;
        Rebalancer rebalancer;
        object positionLocker = new object();
        HashSet<DateTime> rebalanceTimes = new HashSet<DateTime>();
        #endregion

        #region public interface
        public CryptoQuantoCorrStrategy(IExchange exch, IQuotingServiceFactory qFactory, 
                                        string ethbtc, string eth, 
                                        string ethfut, string btc, string btcfut)
        {
            exchange = exch;
            ethbtcInstr = ethbtc;
            ethInstr = eth;
            btcInstr = btc;
            ethFutureInstr = ethfut;
            btcFutureInstr = btcfut;
            this.qFactory = qFactory;
        }

        public void Start()
        {
            log.Info("Strategy started");
            quoter = qFactory.CreateService(exchange, ethbtcInstr, 5);
            ethHedger = new ETHHedgingService(exchange, ethInstr, ethFutureInstr);
            btcHedger = new BTCHedgingService(exchange, btcInstr, btcFutureInstr);

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
        #endregion

        #region private
        Tuple<decimal, decimal> ComputeETHBTCBidAsk()
        {
            var ethP = exchange.MarketDataSystem.GetBidAsk(ethInstr);
            var ethFP = exchange.MarketDataSystem.GetBidAsk(ethFutureInstr);
            var xbtP = exchange.MarketDataSystem.GetBidAsk(btcInstr);
            var xbtFP = exchange.MarketDataSystem.GetBidAsk(btcFutureInstr);
            log.Info(string.Format("{0} bidAsk = {1}/{2}, {3} bidAsk={4}/{5}", ethInstr, ethP.Item1, ethP.Item2, btcInstr, xbtP.Item1, xbtP.Item2));
            log.Info(string.Format("{0} bidAsk = {1}/{2}, {3} bidAsk={4}/{5}", ethFutureInstr, ethFP.Item1, ethFP.Item2, btcFutureInstr, xbtFP.Item1, xbtFP.Item2));

            return Pricer.ComputeETHBTCBidAsk(ethP, ethFP, xbtP, xbtFP);
        }

        private void Run()
        {
            log.Info("Strategy running");
            while (!stop)
            {
                try
                {
                    decimal baseQuantity = 10;
                    var ethBtcBidAsk = ComputeETHBTCBidAsk();
                    var pos = exchange.PositionSystem.GetPosition(ethbtcInstr);
                    decimal posQty = 0;

                    if (pos != null)
                    {
                        posQty = pos.CurrentQty;
                    }

                    if (posQty == 0)
                    {
                        quoter.SetQuote(0, baseQuantity, 0, 0, ethBtcBidAsk.Item1, ethBtcBidAsk.Item2);
                    }
                    else if (posQty > 0)
                    {
                        quoter.SetQuote(0, baseQuantity + posQty, 0, 0, ethBtcBidAsk.Item1, ethBtcBidAsk.Item2);
                    }
                    else
                    {
                        quoter.SetQuote(0, Math.Max(0, baseQuantity - Math.Abs(posQty)), 0, 0, ethBtcBidAsk.Item1, ethBtcBidAsk.Item2);
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


        private void OnPositionUpdate(PositionUpdate update)
        {
            log.Info("OnPositionUpdate");

            lock (positionLocker)
            {
                if (update.symbol == ethbtcInstr && oldQty != update.currentQty)
                {
                    log.Info("ETHBTC Quantity changed");
                    var P = exchange.MarketDataSystem.GetBidAsk(ethbtcInstr);
                    var notional = 0m;
                    if (update.currentQty > 0)
                    {
                        notional = update.currentQty * P.Item2;
                        rebalanceTimes.Clear();
                        log.Info("Positive position, rebalancing cleared");
                    }
                    else if (update.currentQty < 0)
                    {
                        notional = update.currentQty * P.Item1;
                        rebalanceTimes.Add(DateTime.UtcNow.AddHours(1));
                        log.Info("Rebalance added");
                    }

                    log.Info(string.Format("About to hedge notional", notional));
                    ethHedger.Target(-notional);
                    btcHedger.Target(notional);
                    oldQty = update.currentQty;
                }
            }
        }
        #endregion
    }
}
