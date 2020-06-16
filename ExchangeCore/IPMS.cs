using BitmexCore.Dtos;
using ServiceCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeCore
{
    public class PositionUpdate
    {
        public string symbol;
        public decimal currentQty;
        public decimal avgPrice;
    }

    public class Liquidation
    {
        public string symbol;
    }

    public interface IPMS : IBaseService
    {
        void Unsubscribe(string symbol);
        void Subscribe(string symbol, Action<PositionUpdate> callback);

        void SubscribeForLiquidationEvents(Action<Liquidation> callback);
        PositionDto GetPosition(string symbol);
        PositionDto QueryPositionFromExchange(string symbol);
    }
}
