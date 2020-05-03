using Exchange;
using Hedger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Atilla
{
    class Rebalancer
    {
        private BitmexExchange exchange;
        HedgingService ethService;
        HedgingService btcService;
        string ethInstr;
        string btcInstr;
        private ConcurrentDictionary<DateTime, Tuple<decimal, decimal, decimal>> rebalancePeriods;
        private Timer timer;
        private int period;
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(Rebalancer));

        public Rebalancer(int period, BitmexExchange exch, string ethSym, string btcSym, HedgingService eth, HedgingService btc, 
            ConcurrentDictionary<DateTime, Tuple<decimal, decimal, decimal>> rebalances)
        {
            this.period = period;
            exchange = exch;
            ethService = eth;
            btcService = btc;
            ethInstr = ethSym;
            btcInstr = btcSym;
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

            foreach(var interval in rebalancePeriods.ToArray())
            {
                if (now >= interval.Key)
                {
                    log.Info(string.Format("Found rebalance {0}, {1}, {2}, {3}", interval.Key,
                        interval.Value.Item1, interval.Value.Item2, interval.Value.Item3));
                    Tuple<decimal, decimal, decimal> rebalances = null;
                    if (rebalancePeriods.TryRemove(interval.Key, out rebalances))
                    {
                        var newPeriod = Rebalance(rebalances);
                        
                        if (newPeriod != null)
                        {
                            var newInterval = interval.Key.AddMilliseconds((double)period);
                            if (rebalancePeriods.TryAdd(newInterval, newPeriod))
                            {
                                log.Info(string.Format("rebalance updated to {0}, {1}, {2}, {3}", newInterval, newPeriod.Item1, newPeriod.Item2, newPeriod.Item3));
                            }
                        }
                    }
                }
            }
        }

        private Tuple<decimal, decimal, decimal> Rebalance(Tuple<decimal, decimal, decimal> rebalances)
        {
            try
            {
                var ethP = exchange.MarketDataSystem.GetBidAsk(ethInstr);
                var btcP = exchange.MarketDataSystem.GetBidAsk(btcInstr);
                decimal ethQty = 0;
                decimal btcQty = 0;
                Tuple<decimal, decimal, decimal> retval = null;


                if (rebalances.Item1 < 0)
                {
                    ethQty = -Math.Round(rebalances.Item1 / btcP.Item1 / 0.000001m);
                    btcQty = Math.Round(rebalances.Item1 * ethP.Item2);
                    retval = new Tuple<decimal, decimal, decimal>(rebalances.Item1, ethQty, btcQty);
                    ethService.Add(ethQty - rebalances.Item2);
                    btcService.Add(btcQty - rebalances.Item3);
                    log.Info(string.Format("Rebalanced to ETH={0}, BTC={1}", ethQty, btcQty));
                }
                else
                {
                    log.Info("Positive balance  - no rebalance required");
                }

                return retval;
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
