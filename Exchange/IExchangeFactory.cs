using Bitmex.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange
{
    public interface IExchangeFactory
    {
        IExchange CreateExchange(string key, string secret, bool isProd,
                                        HashSet<string> symbols, HashSet<string> indices,
                                        HashSet<string> fundingInstruments, List<int> fundingHours);
        IFundingFeed CreateFundingFeed(IBitmexApiService svc, HashSet<string> symbols, List<int> fundingHours);
        IMDS CreateMarketDataSystem(IBitmexApiSocketService svc, HashSet<string> symbols, HashSet<string> indices);
        IPMS CreatePositionManagementSystem(IBitmexApiSocketService ssvc, IBitmexApiService rsvc);
        IOMS CreateOrderManagementSystem(IBitmexApiSocketService ssvc, IBitmexApiService rsvc);
    }
}
