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
    public class BitmexExchange
    {
        private readonly string bitmexKey;
        private readonly string bitmexSecret;
        private readonly bool isLive;
        private IBitmexAuthorization authorization;
        private IBitmexApiService restService;
        private IBitmexApiSocketService socketService;
        private OMS orderManagementService;
        private MDS marketDataService;
        private FundingFeed fundingService;
        private PMS positionService;
        private HashSet<string> tradingSymbols;
        private HashSet<string> indices;
        private HashSet<string> fundingInstruments;
        private List<int> fundingHours;
        private Dictionary<string, List<Action<PositionUpdate>>> subscribers = new Dictionary<string, List<Action<PositionUpdate>>>();
        ILog log = LogManager.GetLogger(typeof(BitmexExchange));

        public BitmexExchange(string key, string secret, bool isProd,
                      HashSet<string> symbols, HashSet<string> indices,
                      HashSet<string> fundingInstruments,
                      List<int> fundingHours)
        {
            bitmexKey = key;
            bitmexSecret = secret;
            isLive = isProd;
            tradingSymbols = symbols;
            this.indices = indices;
            this.fundingInstruments = fundingInstruments;
            this.fundingHours = fundingHours;
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

            marketDataService = new MDS(socketService, tradingSymbols, indices);
            fundingService = new FundingFeed(restService, fundingInstruments, fundingHours);
            orderManagementService = new OMS(socketService, restService);
            positionService = new PMS(socketService, restService);
            
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

        public OMS OrderSystem
        {
            get
            {
                return orderManagementService;
            }
        }

        public FundingFeed FundingSystem
        {
            get
            {
                return fundingService;
            }
        }

        public MDS MarketDataSystem
        {
            get
            {
                return marketDataService;
            }
        }

        public PMS PositionSystem
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
    }
}
