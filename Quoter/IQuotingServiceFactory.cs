using Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quoter
{
    public interface IQuotingServiceFactory
    {
        IQuotingService CreateService(IExchange exch, string symbol, decimal rounder);
    }
}
