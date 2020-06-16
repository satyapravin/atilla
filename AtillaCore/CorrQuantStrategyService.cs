using BitmexCore;
using BitmexCore.Models;
using BitmexRESTApi;
using BitmexWebSocket;
using ExchangeCore;
using Microsoft.Extensions.Logging;
using ServiceCore;
using StrategyCore;
using System;
using System.Net.Http;
using BitmexWebSocket.Models.Socket.Events;

namespace AtillaCore
{
    public class CorrQuantStrategyService : BaseTimerService, ICorrQuantStrategyService
    {
        private decimal _currentETHBTCQuantity = 0;
        private readonly object _positionLocker = new object();
        private readonly double _rebalanceInterval = 3600000;
        private decimal _ethBtcBaseQuoteQuantity = 0;
        private readonly AtillaConfig _config;
        private readonly IInstrumentService _instrumentService;

        private readonly IBitmexApiService _restApiService;
        private readonly WebSocketService _webSocketService;
        private readonly IMDS _marketDataService;
        private readonly IPMS _positionService;
        private readonly IOMS _orderService;
        private readonly IRebalanceService _rebalancer;
        private readonly IQuotingService _ethbtcQuoter;
        private readonly IHedgingService _ethHedger;
        private readonly IHedgingService _btcHedger;
        private static readonly string ETHBTCFuture = "ETHBTCFuture";
        private static readonly string ETHFuture = "ETHFuture";
        private static readonly string ETH = "ETH";
        private static readonly string BTC = "BTC";
        private static readonly string BTCFuture = "BTCFuture";

        private static readonly string _className = typeof(CorrQuantStrategyService).Name;

        public CorrQuantStrategyService(AtillaConfig config, IWebSocketFactory socketFactory,
                                       HttpMessageHandler httpHandler, ILoggerFactory logFactory) : base(_className, 2000, logFactory)
        {
            try
            {
                _config = config;
                _logger.LogInformation("Creating CorrQuantStrategy");
                _ethBtcBaseQuoteQuantity = _config.ETHBTCQuantity;
                _rebalanceInterval = _config.RebalanceInterval;
                _instrumentService = new InstrumentService(_config.Instruments, logFactory);

                BitmexAuthorization authorization = new BitmexAuthorization()
                {
                    BitmexEnvironment = _config.Bitmex.IsProd ? BitmexEnvironment.Prod : BitmexEnvironment.Test,
                    Key = _config.Bitmex.Key,
                    Secret = _config.Bitmex.Secret
                };

                _restApiService = BitmexApiService.CreateDefaultApi(authorization, httpHandler, logFactory);
                _webSocketService = new WebSocketService(authorization, socketFactory, logFactory);
                _webSocketService.OnClosed += OnWebSocketClosed;
                _webSocketService.OnErrorReceived += OnWebSocketErrorReceived;
                _webSocketService.Start();

                _marketDataService = new MarketDataService(_webSocketService, logFactory);
                SubscribeToMarketData();
                _positionService = new PositionService(_webSocketService, _restApiService, logFactory);
                _positionService.Subscribe(_instrumentService.Get(ETHBTCFuture).Code, OnPositionUpdate);
                _positionService.SubscribeForLiquidationEvents(OnLiquidation);
                _orderService = new OrderService(_restApiService, _webSocketService, logFactory);

                _ethHedger = new HedgingService(_instrumentService.Get(ETH).Code,
                                                _instrumentService.Get(ETHFuture).Code,
                                                _orderService,
                                                _positionService,
                                                _marketDataService,
                                                logFactory);

                _btcHedger = new HedgingService(_instrumentService.Get(BTC).Code,
                                                _instrumentService.Get(BTCFuture).Code,
                                                _orderService,
                                                _positionService,
                                                _marketDataService,
                                                logFactory);

                _ethbtcQuoter = new QuotingService(_instrumentService.Get(ETHBTCFuture).Code,
                                                   _marketDataService,
                                                   _positionService,
                                                   _orderService,
                                                   logFactory);

                _rebalancer = new RebalanceService(this, _rebalanceInterval, logFactory);
                _instrumentService.Start();
                _marketDataService.Start();
                _orderService.Start();
                _positionService.Start();
            }
            catch(Exception e)
            {
                _logger.LogCritical(e, "CorrQuantStrategy creation failed");
                throw;
            }
        }

        private void OnWebSocketErrorReceived(object sender, BitmextErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "OnWebSocketError");
        }

        private void OnWebSocketClosed(object sender, BitmexCloseEventArgs e)
        {
            _logger.LogCritical("Websocket closed. Stopping strategy");
            Stop();
        }

        public IQuotingService GetETHBTCQuoter()
        {
            return _ethbtcQuoter;
        }

        public IHedgingService GetETHHedger()
        {
            return _ethHedger;
        }

        public IHedgingService GetBTCHedger()
        {
            return _btcHedger;
        }

        public IRebalanceService GetRebalancer()
        {
            return _rebalancer;
        }

        public IInstrumentService GetInstrumentService()
        {
            return _instrumentService;
        }

        public IMDS GetMarketDataService()
        {
            return _marketDataService;
        }

        public IOMS GetOrderService()
        {
            return _orderService;
        }

        public IPMS GetPositionService()
        {
            return _positionService;
        }

        public WebSocketService GetWebSocketService()
        {
            return _webSocketService;
        }

        public void Rebalance()
        {
            if (_status)
            {
                var ethBtcPos = _positionService.GetPosition(_instrumentService.Get(ETHBTCFuture).Code);
                var eqQty = GetEquilibriumQuantities(ethBtcPos.CurrentQty);
                bool basePositive = ethBtcPos.CurrentQty > 0;
                Hedge(eqQty.Item1, eqQty.Item2, basePositive);
            }
            else
            {
                throw new ApplicationException("Strategy not running; cannot rebalance");
            }
        }

        protected override bool StartService()
        {
            lock(_positionLocker)
            {
                _ethBtcBaseQuoteQuantity = _config.ETHBTCQuantity;
            }

            _ethHedger.Start();
            _btcHedger.Start();
            _ethbtcQuoter.Start();
            _rebalancer.Start();
            return base.StartService();
        }

        protected override bool StopService()
        {
            _status = false;
            lock(_positionLocker)
            {
                _ethBtcBaseQuoteQuantity = 0;
            }

            _ethHedger.Stop();
            _btcHedger.Stop();
            _ethbtcQuoter.Stop();
            _rebalancer.Stop();
            return base.StopService();
        }

        protected override void OnTimer()
        {
            try
            {
                if (_status)
                {
                    Tuple<decimal, decimal> ethBtcBidAsk = ComputeETHBTCBidAsk();
                    var pos = _positionService.GetPosition(_instrumentService.Get(ETHBTCFuture).Code);
                    decimal posQty = 0;
                    decimal baseQuoteQty = 0;
                    lock(_positionLocker) { baseQuoteQty = _ethBtcBaseQuoteQuantity; }

                    if (pos != null)
                    {
                        posQty = pos.CurrentQty;
                    }
                    else
                    {
                        try
                        {
                            pos = _positionService.QueryPositionFromExchange(_instrumentService.Get(ETHBTCFuture).Code);
                        }
                        catch(Exception ex) 
                        { 
                            _logger.LogError(ex, "Failed to fetch position"); 
                        }
                        
                        posQty = pos != null ? pos.CurrentQty : 0;
                    }

                    if (posQty == 0)
                    {
                        _ethbtcQuoter.SetAskQuote(baseQuoteQty, ethBtcBidAsk.Item2, 0, _instrumentService.Get(ETHBTCFuture).TickSize);
                    }
                    else
                    {
                        _ethbtcQuoter.SetAskQuote(Math.Max(0, baseQuoteQty + posQty), ethBtcBidAsk.Item2, 0,
                                               _instrumentService.Get(ETHBTCFuture).TickSize);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex, "Strategy OnTimer Failed");
            }
        }

        public Tuple<decimal, decimal> ComputeETHBTCBidAsk()
        {
            var ethP = _marketDataService.GetBidAsk(_instrumentService.Get(ETH).Code);
            var ethFP = _marketDataService.GetBidAsk(_instrumentService.Get(ETHFuture).Code);
            var xbtP = _marketDataService.GetBidAsk(_instrumentService.Get(BTC).Code);
            var xbtFP = _marketDataService.GetBidAsk(_instrumentService.Get(BTCFuture).Code);
            _logger.LogInformation(string.Format("{0} bidAsk = {1}/{2}, {3} bidAsk={4}/{5}", ETH, ethP.Item1, ethP.Item2, BTC, xbtP.Item1, xbtP.Item2));
            _logger.LogInformation(string.Format("{0} bidAsk = {1}/{2}, {3} bidAsk={4}/{5}", ETHFuture, ethFP.Item1, ethFP.Item2, BTCFuture, xbtFP.Item1, xbtFP.Item2));

            return AtillaPricer.ComputeETHBTCBidAsk(ethP, ethFP, xbtP, xbtFP);
        }

        private void SubscribeToMarketData()
        {
            _marketDataService.Register(new string[]
            {
                    _instrumentService.Get(ETH).Code,
                    _instrumentService.Get(BTC).Code,
                    _instrumentService.Get(ETHFuture).Code,
                    _instrumentService.Get(BTCFuture).Code,
                    _instrumentService.Get(ETHBTCFuture).Code
            });
        }

        private void Hedge(decimal ethQty, decimal btcQty, bool basePositive)
        {
            _ethHedger.AcquirePosition(ethQty, !basePositive);
            _btcHedger.AcquirePosition(btcQty, basePositive);
            _ethbtcQuoter.Close();
        }

        private void CloseAllPositions()
        {
            _ethHedger.AcquirePosition(0, false);
            _btcHedger.AcquirePosition(0, false);
        }

        private void OnLiquidation(Liquidation liquidation)
        {
            try
            {
                CloseAllPositions();
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Close all positions excepted");
            }
        }
        private void OnPositionUpdate(PositionUpdate update)
        {
            try
            {
                decimal ethTargetPosition = 0;
                decimal btcTargetPosition = 0;
                bool ethbtcPositivePosition = false;
                bool posSet = false;

                lock (_positionLocker)
                {
                    if (update.symbol == _instrumentService.Get(ETHBTCFuture).Code && _currentETHBTCQuantity != update.currentQty)
                    {
                        _logger.LogInformation("ETHBTC Quantity changed");
                        var eqQty = GetEquilibriumQuantities(update.currentQty);
                        ethTargetPosition = eqQty.Item1;
                        btcTargetPosition = eqQty.Item2;
                        _currentETHBTCQuantity = update.currentQty;
                        ethbtcPositivePosition = _currentETHBTCQuantity > 0;
                        posSet = true;
                    }
                }

                if (posSet)
                {
                    Hedge(ethTargetPosition, btcTargetPosition, ethbtcPositivePosition);
                }
            }
            catch(Exception e)
            {
                _logger.LogCritical(e, "OnPositionUpdated failed");
            }
        }


        public Tuple<decimal, decimal> GetEquilibriumQuantities(decimal ethNotional)
        {
            var btcTargetPosition = AtillaPricer.GetBTCQuantityFromETHBTCQuantity(ethNotional,
                                           _marketDataService.GetBidAsk(_instrumentService.Get(ETH).Code));
            var ethTargetPosition = AtillaPricer.GetETHQuantityFromETHBTCQuantity(-ethNotional,
                                           _marketDataService.GetBidAsk(_instrumentService.Get(BTC).Code));
            return new Tuple<decimal, decimal>(ethTargetPosition, btcTargetPosition);
        }

        public AtillaConfig GetConfig()
        {
            return _config;
        }

        public void SetBaseETHBTCQuantity(decimal qty)
        {
            if (qty >= 0)
            {
                lock (_positionLocker)
                {
                    _ethBtcBaseQuoteQuantity = qty;
                }
            }
        }
    }
}
