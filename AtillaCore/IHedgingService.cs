using ServiceCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AtillaCore
{
    public interface IHedgingService : IBaseService
    {
        public void AcquirePosition(decimal target, bool basePositionPositive);
    }
}
