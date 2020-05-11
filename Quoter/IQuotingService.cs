using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quoter
{
    public interface IQuotingService
    {
        void SetQuote(decimal bidQ, decimal askQ, decimal bidSpread, decimal askSpread, decimal bidP, decimal askP);

        void Start();

        void Stop();
    }
}
