using ServiceCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeCore
{
    public interface IQuotingService : IBaseService
    {
        void SetQuote(decimal bidQty, decimal bidPrice, decimal bidSpread,
                      decimal askQty, decimal askPrice, decimal askSpread, decimal tickSize);
    }
}
