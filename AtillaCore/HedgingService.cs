using BitmexRESTApi;
using ExchangeCore;
using Microsoft.Extensions.Logging;
using ServiceCore;
using System;
using System.Timers;

namespace AtillaCore
{
    public class HedgingService : BaseTimerService, IHedgingService
    {
        #region private members
        private readonly IOMS _orderService;
        private readonly IPMS _positionService;
        private readonly IMDS _marketDataService;
        private string _symbol;
        private string _future;
        private decimal _targetPosition;
        private bool _basePositionPositive;
        private volatile bool _set = false;
        private object _locker = new object();

        private static readonly string _className = typeof(HedgingService).Name;
        #endregion

        #region public members
        public static void Hedge(string symbol, string future, IMDS mds, IPMS pms, IOMS oms,
                                 decimal targetPos, bool basePositive, ILogger logger)
        {
            var spotPosition = pms.GetPosition(symbol);
            var futurePosition = pms.GetPosition(future);

            if (spotPosition == null)
            {
                logger.LogError(string.Format("Spot position not found, querying exchange for {0}", symbol));
                spotPosition = pms.QueryPositionFromExchange(symbol);
            }

            if (futurePosition == null)
            {
                logger.LogError(string.Format("Future position not found, querying exchange for {0}", future));
                futurePosition = pms.QueryPositionFromExchange(future);
            }

            var currSpotQty = 0m;
            var currFutQty = 0m;
            if (spotPosition != null)
                currSpotQty = spotPosition.CurrentQty;

            if (futurePosition != null)
                currFutQty = futurePosition.CurrentQty;

            if (targetPos == 0)
            {
                logger.LogInformation("Target is 0 closing all positions");
                
                if (currSpotQty != 0)
                {
                    logger.LogInformation(string.Format("Closing {0} of {1}", currSpotQty, symbol));
                    NewMarketOrder(symbol, -currSpotQty, oms);
                }

                if (currFutQty != 0)
                {
                    logger.LogInformation(string.Format("Closing {0} of {1}", currFutQty, future));
                    NewMarketOrder(future, -currFutQty, oms);
                }

                return;
            }

            logger.LogInformation(string.Format("Current quantity {0}={1}, {2}={3}", symbol, currSpotQty, future, currFutQty));
            var spotP = mds.GetBidAsk(symbol);
            var futP = mds.GetBidAsk(future);

            targetPos -= currSpotQty;
            targetPos -= currFutQty;
            logger.LogInformation(string.Format("Trade position={0} for {1}/{2}", targetPos, symbol, future));

            if (targetPos != 0)
            {
                string tradeSymbol;

                Tuple<decimal, decimal> refPrice = null;

                if (spotP.Item1 < futP.Item1)
                {
                    if (targetPos > 0)
                    {
                        if (basePositive)
                        {
                            tradeSymbol = symbol;
                            refPrice = spotP;
                        }
                        else if (Math.Abs(currSpotQty) < Math.Abs(currFutQty))
                        {
                            tradeSymbol = symbol;
                            refPrice = spotP;
                        }
                        else
                        {
                            tradeSymbol = future;
                            refPrice = futP;
                        }
                    }
                    else
                    {
                        if (!basePositive)
                        {
                            tradeSymbol = future;
                            refPrice = futP;
                        }
                        else if (Math.Abs(currFutQty) < Math.Abs(currSpotQty))
                        {
                            tradeSymbol = future;
                            refPrice = futP;
                        }
                        else
                        {
                            tradeSymbol = symbol;
                            refPrice = spotP;
                        }
                    }
                }
                else
                {
                    if (targetPos > 0)
                    {
                        if (basePositive)
                        {
                            tradeSymbol = future;
                            refPrice = futP;
                        }
                        else if (Math.Abs(currFutQty) < Math.Abs(currSpotQty))
                        {
                            tradeSymbol = future;
                            refPrice = futP;
                        }
                        else
                        {
                            tradeSymbol = symbol;
                            refPrice = spotP;
                        }
                    }
                    else
                    {
                        if (!basePositive)
                        {
                            tradeSymbol = symbol;
                            refPrice = spotP;
                        }
                        if (Math.Abs(currSpotQty) < Math.Abs(currFutQty))
                        {
                            tradeSymbol = symbol;
                            refPrice = spotP;
                        }
                        else
                        {
                            tradeSymbol = future;
                            refPrice = futP;
                        }
                    }
                }

                logger.LogInformation(string.Format("Selected {0} for quantities {1}/{2} and prices {3}/{4}",
                    tradeSymbol, currSpotQty, currFutQty, spotP.Item1, futP.Item1));
            }
        }

        public HedgingService(string sym, 
                              string fut,
                              IOMS ordSvc, 
                              IPMS posSvc, 
                              IMDS mdsSvc,
                              ILoggerFactory factory):base(_className, 2000, factory)
        {
            _symbol = sym;
            _future = fut;
            _orderService = ordSvc;
            _positionService = posSvc;
            _marketDataService = mdsSvc;
        }
        public void AcquirePosition(decimal target, bool basePositive)
        {
            lock (_locker) { _targetPosition = target; _basePositionPositive = basePositive; _set = true; }
        }
        #endregion

        #region protected members
        protected override void OnTimer()
        {
            try
            {
                if (!_set) return;
                decimal targetPos = 0;
                bool basePositive = false;
                lock (_locker) { targetPos = _targetPosition; basePositive = _basePositionPositive; }
                Hedge(_symbol, _future, _marketDataService, _positionService, _orderService, targetPos, basePositive, _logger);                    
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Hedging service failed OnTimer");
            }
        }
        private static void NewMarketOrder(string tradeSymbol, decimal tradeQty, IOMS oms)
        {
            oms.NewOrder(
                new OrderRequest()
                {
                    orderType = OrderType.NEW_MARKET_ORDER,
                    quantity = Math.Abs(tradeQty),
                    symbol = tradeSymbol,
                    side = tradeQty > 0 ? OrderSide.BUY : OrderSide.SELL
                });
        }
        #endregion
    }
}
