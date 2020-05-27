using Microsoft.Extensions.Logging;
using System;
using BitmexWebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using BitmexWebSocket.Dtos.Socket;
using BitmexWebSocket.Models.Socket.Events;
using ServiceCore;

namespace ExchangeCore
{
    public class MarketDataService : BaseService, IMDS
    {
        #region private members
        private class Table
        {
            public decimal[][] bids = null;
            public decimal[][] asks = null;
            public Table(decimal[][] bidarray, decimal[][] askarray) 
            { bids = bidarray; asks = askarray; }
            public Tuple<decimal, decimal> GetBidAsk()
            {
                return new Tuple<decimal, decimal>(bids[0][0], asks[0][0]);
            }
        }
        private readonly WebSocketService _socketService;
        private readonly HashSet<string> _symbols = new HashSet<string>();
        private ConcurrentDictionary<string, Table> _books = new ConcurrentDictionary<string, Table>();
        private static string _classname = typeof(MarketDataService).Name;
        #endregion

        #region public methods
        public MarketDataService(WebSocketService socketSvc, ILoggerFactory logFactory) 
               : base(_classname, logFactory) 
        {
            _socketService = socketSvc;
            _socketService.Stopped += _socketService_Stopped;
            _socketService.OnClosed += _socketService_OnClosed;
        }

        private void _socketService_OnClosed(object sender, BitmexCloseEventArgs e)
        {
            Stop();
        }

        private void _socketService_Stopped(object sender, EventArgs e)
        {
            Stop();   
        }

        public Tuple<decimal, decimal> GetBidAsk(string symbol)
        {
            if (_status)
            {
                if (_books.TryGetValue(symbol, out Table t))
                {
                    return t.GetBidAsk();
                }
                else
                {
                    _logger.LogError("Market data (Bid/Ask) not found for {symbol}", symbol);
                    throw new ApplicationException("Market data not found");
                }
            }
            else
            {
                throw new ApplicationException("Market data service not running");
            }
        }

        public void Register(IEnumerable<string> symbols)
        {
            _logger.LogTrace("Registering symbols {count}", symbols.Count());

            if (_status)
            {
                throw new ApplicationException("cannot register when service in running");
            }

            foreach (var sym in symbols)
            {
                if (!_symbols.Contains(sym))
                {
                    _symbols.Add(sym);
                } 
                else
                {
                    _logger.LogInformation("{sym} already registered", sym);
                }
            }
        }

        #endregion

        #region protected
        protected override bool StartService()
        {
            SubscribeAll();
            return true;
        }

        protected override bool StopService()
        {

            UnsubscribeAll();
            return true;
        }

        #endregion

        #region private members
        private void SubscribeAll()
        {
            _logger.LogInformation("Subscribing to market data");

            foreach (var sym in _symbols)
            {
                _logger.LogInformation("Subscribing to {sym}", sym);
                _symbols.Add(sym);
                _socketService.Subscribe(BitmexSocketSubscriptions.CreateOrderBook10Subsription(
                message =>
                {
                    Post(message.Data);
                }, new object[] { sym }));
            }
        }

        public void UnsubscribeAll()
        {
            _logger.LogTrace("Unregistering all symbols");
            try
            {
                foreach (var sym in _symbols)
                {
                    _socketService.Unsubscribe(BitmexSocketSubscriptions.CreateOrderBook10Subsription(
                        message => { }, new object[] { sym }));
                    _logger.LogInformation("{sym} unsubscribed", sym);
                }
            }
            catch(Exception e)
            {
                _logger.LogInformation(e, "Exception on unregister all symbols");
            }
        }

        private void Post(IEnumerable<OrderBook10Dto> dtos)
        {
            foreach (var d in dtos)
            {
                Sort(d.Asks, 0);
                ReverseSort(d.Bids, 0);
                var t = new Table(d.Bids, d.Asks);
                _books.AddOrUpdate(d.Symbol, t, (k, v) => t);
            }
        }

        private static void Sort<T>(T[][] data, int col)
        {
            Comparer<T> comparer = Comparer<T>.Default;
            Array.Sort<T[]>(data, (x, y) => comparer.Compare(x[col], y[col]));
        }

        private static void ReverseSort<T>(T[][] data, int col)
        {
            Comparer<T> comparer = Comparer<T>.Default;
            Array.Sort<T[]>(data, (x, y) => comparer.Compare(y[col], x[col]));
        }
        #endregion
    }
}
