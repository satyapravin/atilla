using ServiceCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeCore
{
    public interface IQuotingService : IBaseService
    {
        void SetAskQuote(decimal askQty, decimal askPrice, decimal askSpread, decimal tickSize);
        void Close();
    }
}
