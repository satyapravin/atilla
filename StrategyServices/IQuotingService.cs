using ServiceCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeCore
{
    public interface IQuotingService : IBaseService
    {
        void SetBidQuote(decimal bidQty, decimal bidPrice, decimal bidSpread, decimal tickSize);
        void SetAskQuote(decimal askQty, decimal askPrice, decimal askSpread, decimal tickSize);
        void Close();
    }
}
