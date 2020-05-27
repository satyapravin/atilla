using ServiceCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AtillaCore
{
    public interface IRebalanceService : IBaseService
    {
        void Rebalance();
    }
}
