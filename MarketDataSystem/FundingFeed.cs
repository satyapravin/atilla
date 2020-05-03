using System;
using System.Collections.Generic;
using System.Linq;
using Bitmex.NET;
using Bitmex.NET.Dtos;
using System.Threading;
using System.Collections.Concurrent;

namespace Exchange
{
    public class FundingFeed
    {
        IBitmexApiService bitmexService;
        HashSet<string> instruments;
        List<int> fundingHours;
        Timer timer;
        ConcurrentDictionary<string, decimal> indicativeFundingRates = new ConcurrentDictionary<string, decimal>();
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(FundingFeed));
        public FundingFeed(IBitmexApiService svc, HashSet<string> instruments, List<int> fundingHours)
        {
            bitmexService = svc;
            this.instruments = instruments;
            this.fundingHours = fundingHours;
            this.fundingHours.Sort();

            foreach(var fundingHour in fundingHours)
            {
                log.Info(string.Format("Funding at {0}", fundingHour));
            }
        }

        public void Start()
        {
            log.Info("Funding feed started");
            timer = new Timer(new TimerCallback(onElapsed), null, 0, 60000);

        }

        public decimal getPrediction(string instrument)
        {
            decimal rate;
            if (indicativeFundingRates.TryGetValue(instrument, out rate))
            {
                log.Info(string.Format("getPrediction {0}", rate));
                return rate;
            }

            log.Error("getPrediction funding rate not found");
            return -1;
        }

        public int getMinutesToNextFunding()
        {
            try
            {
                var now = DateTime.UtcNow;
                int retval = 24 * 60;

                var minutes = now.Hour * 60 + now.Minute;
                if (minutes < fundingHours[0])
                {
                    retval = fundingHours[0] * 60 - minutes;
                }
                else if (minutes > fundingHours.Last())
                {
                    retval = 24 * 60 - minutes + fundingHours[0] * 60;
                }
                else
                {
                    var hour = (from fh in fundingHours where fh > now.Hour select fh).First();
                    retval = hour * 60 - minutes;
                }

                log.Info(string.Format("getMinutesToNextFunding {0}", retval));
                return retval;
            }
            catch(Exception e)
            {
                log.Fatal("Funding hours not set", e);
                throw e;
            }
        }

        public void Stop()
        {
            timer.Dispose();
            log.Info("FundingFeed stopped");
        }

        private void onElapsed(object state)
        {
            log.Info("Funding feed timer elapsed");
            foreach(var sym in instruments)
            {
                fetchIndicativeFundingRate(sym);
            }
        }

        private void fetchIndicativeFundingRate(string symbol)
        {
            try
            {
                var param = new Bitmex.NET.Models.InstrumentGETRequestParams();
                param.Symbol = symbol;
                log.Info(string.Format("Requesting funding rate for symbol {0}", symbol));
                var task = bitmexService.Execute(BitmexApiUrls.Instrument.GetInstrument, param);
                task.Wait(10000);
                if (task.IsCompleted)
                {
                    foreach (var dto in task.Result.Result)
                    {
                        if (dto.IndicativeFundingRate.HasValue)
                        {
                            var sym = dto.Symbol;
                            var rate = dto.IndicativeFundingRate.Value;
                            indicativeFundingRates.AddOrUpdate(sym, rate, (k, v) => rate);
                        }
                        else
                        {
                            log.Error("No funding rate in response from Bitmex");
                        }
                    }
                }
                else
                {
                    log.Error("Funding rate request timed out in 10 seconds");
                }
            }
            catch(Exception e)
            {
                log.Error("Funding rate fetch failure", e);
            }
        }
    }
}
