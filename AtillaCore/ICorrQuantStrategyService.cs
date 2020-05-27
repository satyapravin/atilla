using ExchangeCore;
using ServiceCore;
using StrategyCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AtillaCore
{
    public interface ICorrQuantStrategyService : IBaseService
    {
        public IQuotingService GetETHBTCQuoter();
        public IHedgingService GetETHHedger();
        public IHedgingService GetBTCHedger();
        public IRebalanceService GetRebalancer();
        public IInstrumentService GetInstrumentService();
        public IPMS GetPositionService();
        public IOMS GetOrderService();
        public IMDS GetMarketDataService();
        public WebSocketService GetWebSocketService();
        public AtillaConfig GetConfig();
        public void SetBaseETHBTCQuantity(decimal qty);
        public void Rebalance();
    }
}
