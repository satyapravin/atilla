using System;

namespace Exchange
{
    public interface IExchange
    {
        #region public interface

        void PositionSubscribe(string symbol, Action<PositionUpdate> callback);

        void Start();
        IOMS OrderSystem { get; }
        
        IFundingFeed FundingSystem { get; }

        IMDS MarketDataSystem { get; }

        IPMS PositionSystem { get; }

        void Stop();
        #endregion
    }
}
