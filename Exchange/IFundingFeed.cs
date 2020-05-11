using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange
{
    public interface IFundingFeed
    {
        void Start();
        decimal getPrediction(string instrument);
        int getMinutesToNextFunding();
        void Stop();
        
    }
}
