using ExchangeCore;
using Microsoft.Extensions.Logging;
using ServiceCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace AtillaCore
{
    class RebalanceService : BaseTimerService, IRebalanceService
    {
        private readonly ICorrQuantStrategyService _corrQuantStrategyService;
        private static readonly string _className = typeof(RebalanceService).Name;
        public RebalanceService(ICorrQuantStrategyService svc,
                                double interval, ILoggerFactory factory):base(_className, interval, factory)
        {
            _corrQuantStrategyService = svc;
        }

        public void Rebalance()
        {
            if (_status)
                _corrQuantStrategyService.Rebalance();
            else
                throw new ApplicationException("Service not running");
        }

        protected override void OnTimer()
        {
            try
            {
                if (_status)
                {
                    Rebalance();
                }
            }
            catch(Exception e)
            {
                _logger.LogCritical(e, "Rebalance timer threw up");
            }
        }
    }
}
