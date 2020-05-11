using Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quoter
{
    public class QuotingServiceFactory : IQuotingServiceFactory
    {
        public IQuotingService CreateService(IExchange exch, string symbol, decimal rounder)
        {
            return new QuotingService(exch, symbol, rounder);
        }
    }
}
