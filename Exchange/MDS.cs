using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

using Bitmex.NET;
using Bitmex.NET.Dtos;
using Bitmex.NET.Dtos.Socket;

namespace Exchange
{
    class MDS : IMDS
    {
        #region private members
        private class Table
        {
            public decimal[][] bids;
            public decimal[][] asks;

            public Tuple<decimal, decimal> GetBidAsk()
            {
                return new Tuple<decimal, decimal>(bids[0][0], asks[0][0]);
            }
        }

        private readonly IBitmexApiSocketService bitmexService;
        private readonly HashSet<string> symbols;
        private readonly HashSet<string> indices;
        private ConcurrentDictionary<string, Table> books = new ConcurrentDictionary<string, Table>();
        private ConcurrentDictionary<string, decimal> latests = new ConcurrentDictionary<string, decimal>();
        private log4net.ILog log = log4net.LogManager.GetLogger(typeof(MDS));
        #endregion

        #region public methods
        public MDS(IBitmexApiSocketService service, HashSet<string> symbols, HashSet<string> indices)
        {
            bitmexService = service;
            this.symbols = symbols;
            this.indices = indices;
        }

        public void Start()
        {
            try
            {
                log.Info("MDS starting");
                if (symbols.Count > 0)
                {
                    log.Info("MDS subscribing to symbols");
                    bitmexService.Subscribe(BitmexSocketSubscriptions.CreateOrderBook10Subsription(message =>
                    {
                        Post(message.Data);
                    }, symbols.ToArray<object>()));
                }

                if (indices.Count > 0)
                {
                    log.Info("MDS subscribing to indices");
                    bitmexService.Subscribe(BitmexSocketSubscriptions.CreateInstrumentSubsription(message =>
                    {
                        Post(message.Data);
                    }, indices.ToArray<object>()));
                }
            }
            catch (Exception e)
            {
                log.Fatal("MDS failed to start", e);
                throw e;
            }
        }

        public void Stop()
        {
            log.Info("MDS stopped");
        }

        public Tuple<decimal, decimal> GetBidAsk(string symbol)
        {
            Table t;
            if (books.TryGetValue(symbol, out t))
            {
                return t.GetBidAsk();
            }
            else
            {
                log.Fatal(string.Format("Market data (Bid/Ask) not found for symbol {0}", symbol));
                throw new ApplicationException("Market data not found");
            }
        }

        public decimal GetIndexLast(string index)
        {
            decimal last;

            if (latests.TryGetValue(index, out last))
            {
                return last;
            }
            else
            {
                log.Fatal(string.Format("Index data (Last) not found for symbol {0}", index));
                throw new ApplicationException("Index data not found");
            }
        }
        #endregion

        #region private methods
        private void Post(IEnumerable<InstrumentDto> dtos)
        {
            foreach(var d in dtos)
            {
                if (d.LastPrice.HasValue)
                {
                    latests.AddOrUpdate(d.Symbol, d.LastPrice.Value, (k, v) => d.LastPrice.Value);
                }
            }
        }

        private void Post(IEnumerable<OrderBook10Dto> dtos)
        {
            foreach(var d in dtos)
            {
                Sort(d.Asks, 0);
                ReverseSort(d.Bids, 0);
                var t = new Table() { asks = d.Asks, bids = d.Bids };
                books.AddOrUpdate(d.Symbol, t, (k, v) => t);
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
