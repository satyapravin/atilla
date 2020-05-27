using ServiceCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeCore
{
    public interface IMDS : IBaseService
    {
        public Tuple<decimal, decimal> GetBidAsk(string symbol);
        public void Register(IEnumerable<string> symbols);
    }
}