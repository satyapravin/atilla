using Bitmex.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange
{
    public class ExchangeFactory : IExchangeFactory
    {
        public IExchange CreateExchange(string key, string secret, bool isProd,
                                        HashSet<string> symbols, HashSet<string> indices,
                                        HashSet<string> fundingInstruments, List<int> fundingHours)
        {
            return new BitmexExchange(key, secret, isProd, symbols, indices, fundingInstruments, fundingHours, this);
        }
        public IFundingFeed CreateFundingFeed(IBitmexApiService svc, HashSet<string> symbols, List<int> fundingHours)
        {
            return new FundingFeed(svc, symbols, fundingHours);
        }

        public IMDS CreateMarketDataSystem(IBitmexApiSocketService svc, HashSet<string> symbols, HashSet<string> indices)
        {
            return new MDS(svc, symbols, indices);
        }

        public IOMS CreateOrderManagementSystem(IBitmexApiSocketService ssvc, IBitmexApiService rsvc)
        {
            return new OMS(ssvc, rsvc);
        }

        public IPMS CreatePositionManagementSystem(IBitmexApiSocketService ssvc, IBitmexApiService rsvc)
        {
            return new PMS(ssvc, rsvc);
        }
    }
}
