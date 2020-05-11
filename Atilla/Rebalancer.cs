using Exchange;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Atilla
{
    public class Rebalancer
    {
        private IExchange exchange;
        HedgingService ethService;
        HedgingService btcService;
        string ethInstr;
        string btcInstr;
        string ethFutureInstr;
        string btcFutureInstr;
        private HashSet<DateTime> rebalancePeriods;
        private Timer timer;
        private int period;
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(Rebalancer));

        public Rebalancer(int period, IExchange exch, 
            string ethSym, string btcSym,  string ethFuture, string btcFuture,
            HedgingService eth, HedgingService btc, 
            HashSet<DateTime> rebalances)
        {
            this.period = period;
            exchange = exch;
            ethService = eth;
            btcService = btc;
            ethInstr = ethSym;
            ethFutureInstr = ethFuture;
            btcInstr = btcSym;
            btcFutureInstr = btcFuture;
            rebalancePeriods = rebalances;
        }

        public void Start()
        {
            log.Info("Rebalancer started");
            timer = new Timer(OnElapsed, null, 5000, period);
        }

        private void OnElapsed(object state)
        {
            log.Info("Rebalancer timer elapsed");
            var now = DateTime.UtcNow;
            DateTime[] periods = null;

            lock (rebalancePeriods)
            {
                periods = rebalancePeriods.ToArray();
            }

            foreach(var interval in periods)
            {
                if (now >= interval)
                {
                    log.Info(string.Format("Found rebalance {0}", interval));
                    Rebalance();

                    lock (rebalancePeriods)
                    {
                        rebalancePeriods.Remove(interval);
                        var newInterval = interval.AddMilliseconds((double)period);
                        rebalancePeriods.Add(newInterval);
                        log.Info(string.Format("rebalance updated to {0}", newInterval));
                    }
                }
            }
        }

        public void Rebalance()
        {
            try
            {
                    ethService.Rebalance();
                    btcService.Rebalance();
                
            }
            catch(Exception e)
            {
                log.Fatal("Rebalance threw up", e);
                throw;
            }
        }

        public void Stop()
        {
            log.Info("Rebalancer stopping");
            timer.Dispose();
            log.Info("Rebalancer stopped");
        }
    }
}