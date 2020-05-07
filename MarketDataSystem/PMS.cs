using Bitmex.NET;
using Bitmex.NET.Dtos;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using Bitmex.NET.Models;
using Bitmex.NET.Dtos.Socket;

namespace Exchange
{
    public class PositionUpdate
    {
        public string symbol;
        public decimal currentQty;
        public decimal avgPrice;
    }

    public class PMS
    {
        private readonly IBitmexApiService restService;
        private readonly IBitmexApiSocketService socketService;
        private Timer timer;
        private ConcurrentDictionary<string, PositionDto> positionsBySymbol = new ConcurrentDictionary<string, PositionDto>();
        private Dictionary<string, List<Action<PositionUpdate>>> subscribers = new Dictionary<string, List<Action<PositionUpdate>>>();
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(PMS));

        public PMS(IBitmexApiSocketService socketSvc, IBitmexApiService restSvc)
        {
            socketService = socketSvc;
            restService = restSvc;
        }

        public void Subscribe(string symbol, Action<PositionUpdate> callback)
        {
            log.Info(string.Format("Position subscription received for {0}", symbol));

            if (!subscribers.ContainsKey(symbol))
            {
                subscribers[symbol] = new List<Action<PositionUpdate>>();
            }

            subscribers[symbol].Add(callback);
        }

        public void Start()
        {
            log.Info("PMS starting");
            OnElapsed(null);
            log.Info("Subscribing to position updates");
            socketService.Subscribe(BitmexSocketSubscriptions.CreatePositionSubsription(message => Post(message.Action, message.Data)));
            timer = new Timer(new TimerCallback(OnElapsed), null, 0, 10000);
        }

        public void Stop()
        {
            timer.Dispose();
            log.Info("PMS stopped");
        }

        public PositionDto GetPosition(string symbol)
        {
            var dto = new PositionDto();
            if (positionsBySymbol.TryGetValue(symbol, out dto))
            {
                return dto;
            }
            else
            {
                log.Info(string.Format("Position for {0} not found", symbol));
            }

            return null;
        }

        private void OnElapsed(object state)
        {
            log.Info("PMS timer elapsed");
            var positions = GetPositions();
            foreach (var pos in positions)
            {
                AddOrUpdate(pos);
            }
        }

        public PositionDto QueryPositionFromExchange(string symbol)
        {
            try
            {
                log.Info(string.Format("Querying for {0} position", symbol));
                var param = new PositionGETRequestParams();
                param.Filter = new Dictionary<string, string>();
                param.Filter["symbol"] = symbol;
                var task = restService.Execute(BitmexApiUrls.Position.GetPosition, param);
                task.Wait(5000);

                if (task.IsCompleted)
                {
                    return task.Result.Result.First();
                }
                else
                {
                    log.Error(string.Format("Query for {0} position timedout", symbol));
                }
            }
            catch(Exception e)
            {
                log.Fatal(string.Format("Query for {0} position excepted", symbol), e);
            }

            return null;
        }

        private IEnumerable<PositionDto> GetPositions()
        {
            try
            {
                log.Info("Fetching positions for PMS reconciliation");
                PositionGETRequestParams param = new PositionGETRequestParams();
                var task = restService.Execute(BitmexApiUrls.Position.GetPosition, param);
                task.Wait(10000);
                if (task.IsCompleted)
                {
                    return task.Result.Result;
                }
                else
                {
                    log.Error("position fetch timedout in 10 seconds");
                    return new List<PositionDto>();
                }
            }
            catch (Exception e)
            {
                log.Fatal("position fetch error", e);
            }

            return new List<PositionDto>();
        }

        private void AddOrUpdate(PositionDto dto)
        {
            bool updated = true;

            positionsBySymbol.AddOrUpdate(dto.Symbol, dto,
                (k, v) =>
                {
                    if (dto.CurrentTimestamp > v.CurrentTimestamp)
                    {
                        if (!dto.AvgEntryPrice.HasValue)
                        {
                            dto.AvgEntryPrice = v.AvgEntryPrice;
                        }

                        return dto;
                    }
                    else
                    {
                        updated = false;
                        return v;
                    }
                }
            );

            if (updated)
            {
                if (subscribers.ContainsKey(dto.Symbol))
                {
                    PositionUpdate update = new PositionUpdate
                    {
                        symbol = dto.Symbol,
                        currentQty = dto.CurrentQty,
                        avgPrice = dto.AvgEntryPrice.HasValue ? dto.AvgEntryPrice.Value : 0
                    };

                    subscribers[dto.Symbol].ForEach(action => action(update));
                }
            }
        }

        private void Post(BitmexActions action, IEnumerable<PositionDto> dtos)
        {
            foreach(var dto in dtos)
            {
                log.Info(string.Format("On position {0} for {1}", action.ToString(), dto.Symbol));
                if (action == BitmexActions.Delete)
                {
                    positionsBySymbol.TryRemove(dto.Symbol, out _);
                }
                else if (action == BitmexActions.Insert || action == BitmexActions.Update || action == BitmexActions.Partial)
                {
                    AddOrUpdate(dto);
                }
            }
        }
    }
}