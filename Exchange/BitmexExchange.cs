using Bitmex.NET.Models;
using Bitmex.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitmex.NET.Dtos;
using log4net;

namespace Exchange
{
    class BitmexExchange : IExchange
    {
        #region private members
        private readonly string bitmexKey;
        private readonly string bitmexSecret;
        private readonly bool isLive;
        private IBitmexAuthorization authorization;
        private IBitmexApiService restService;
        private IBitmexApiSocketService socketService;
        private IOMS orderManagementService;
        private IMDS marketDataService;
        private IFundingFeed fundingService;
        private IPMS positionService;
        private HashSet<string> tradingSymbols;
        private HashSet<string> indices;
        private HashSet<string> fundingInstruments;
        private List<int> fundingHours;
        private IExchangeFactory factory;
        private Dictionary<string, List<Action<PositionUpdate>>> subscribers = new Dictionary<string, List<Action<PositionUpdate>>>();
        ILog log = LogManager.GetLogger(typeof(BitmexExchange));
        #endregion

        #region public interface
        public BitmexExchange(string key, string secret, bool isProd,
                      HashSet<string> symbols, HashSet<string> indices,
                      HashSet<string> fundingInstruments,
                      List<int> fundingHours, IExchangeFactory factory)
        {
            bitmexKey = key;
            bitmexSecret = secret;
            isLive = isProd;
            tradingSymbols = symbols;
            this.indices = indices;
            this.fundingInstruments = fundingInstruments;
            this.fundingHours = fundingHours;
            this.factory = factory;
        }

        public void PositionSubscribe(string symbol, Action<PositionUpdate> callback)
        {
            if (!subscribers.ContainsKey(symbol))
            {
                subscribers[symbol] = new List<Action<PositionUpdate>>();
            }

            subscribers[symbol].Add(callback);
            log.Info(string.Format("Position subscribed for {0}", symbol));
        }
        public void Start()
        {
            var env = BitmexEnvironment.Test;

            if (this.isLive)
            {
                env = BitmexEnvironment.Prod;
            }

            log.Info(string.Format("Exchange starting in {0} mode", env));
            authorization = new BitmexAuthorization { BitmexEnvironment = env };
            authorization.Key = bitmexKey;
            authorization.Secret = bitmexSecret;
            restService = BitmexApiService.CreateDefaultApi(authorization);
            socketService = BitmexApiSocketService.CreateDefaultApi(authorization);
            
            if (!socketService.Connect())
            {
                log.Fatal("failed to connect to bitmex websocket");
                throw new ApplicationException("Cannot connect bitmex websocket");
            }

            marketDataService = factory.CreateMarketDataSystem(socketService, tradingSymbols, indices);
            fundingService = factory.CreateFundingFeed(restService, fundingInstruments, fundingHours);
            orderManagementService = factory.CreateOrderManagementSystem(socketService, restService);
            positionService = factory.CreatePositionManagementSystem(socketService, restService);
            
            foreach(var sym in subscribers.Keys)
            {
                foreach(var cb in subscribers[sym])
                    positionService.Subscribe(sym, cb);
            }

            orderManagementService.Start();
            marketDataService.Start();
            fundingService.Start();
            positionService.Start();
            log.Info("Exchange started");
        }

        public IOMS OrderSystem
        {
            get
            {
                return orderManagementService;
            }
        }

        public IFundingFeed FundingSystem
        {
            get
            {
                return fundingService;
            }
        }

        public IMDS MarketDataSystem
        {
            get
            {
                return marketDataService;
            }
        }

        public IPMS PositionSystem
        {
            get
            {
                return positionService;
            }
        }

        public void Stop()
        {
            log.Info("Exchange stopping");
            fundingService.Stop();
            marketDataService.Stop();
            orderManagementService.Stop();
            positionService.Stop();
            log.Info("Exchange stopped");
        }
        #endregion
    }
}
