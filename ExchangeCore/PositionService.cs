using BitmexCore.Dtos;
using BitmexCore.Models;
using BitmexRESTApi;
using BitmexWebSocket;
using BitmexWebSocket.Dtos.Socket;
using Microsoft.Extensions.Logging;
using ServiceCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace ExchangeCore
{
    public class PositionService : BaseTimerService, IPMS
    {
        #region private members
        private readonly WebSocketService _socketService;
        private readonly IBitmexApiService _restService;
        private ConcurrentDictionary<string, PositionDto> _positionsBySymbol = new ConcurrentDictionary<string, PositionDto>();
        private Dictionary<string, List<Action<PositionUpdate>>> _subscribers = new Dictionary<string, List<Action<PositionUpdate>>>();
        private static readonly string _classname = typeof(PositionService).Name;
        #endregion

        #region public members
        public PositionService(WebSocketService svc, IBitmexApiService restSvc, ILoggerFactory factory):base(_classname, 5000, factory)
        {
            _restService = restSvc;
            _socketService = svc;
            _socketService.Stopped += _socketService_Stopped;
            _socketService.OnClosed += _socketService_OnClosed;
        }

        private void _socketService_OnClosed(object sender, BitmexWebSocket.Models.Socket.Events.BitmexCloseEventArgs e)
        {
            Stop();
        }

        public PositionDto GetPosition(string symbol)
        {
            if (_status)
            {
                if (_positionsBySymbol.TryGetValue(symbol, out PositionDto dto))
                {
                    return dto;
                }
                else
                {
                    _logger.LogInformation(string.Format("Position for {0} not found", symbol));
                }
            }
            else
            {
                throw new ApplicationException("Service not running");
            }

            return null;
        }

        public PositionDto QueryPositionFromExchange(string symbol)
        {
            try
            {
                _logger.LogInformation(string.Format("Querying for {0} position", symbol));
                var param = new PositionGETRequestParams
                {
                    Filter = new Dictionary<string, string>()
                };
                param.Filter["symbol"] = symbol;
                var task = _restService.Execute(BitmexApiUrls.Position.GetPosition, param);
                task.Wait(5000);

                if (task.IsCompleted)
                {
                    return task.Result.Result.First();
                }
                else
                {
                    _logger.LogError(string.Format("Query for {0} position timedout", symbol));
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, string.Format("Query for {0} position excepted", symbol));
            }

            return null;
        }

        public void Unsubscribe(string symbol)
        {
            _subscribers.Remove(symbol);
        }

        public void Subscribe(string symbol, Action<PositionUpdate> callback)
        {
            _logger.LogInformation(string.Format("Position subscription received for {0}", symbol));

            if (!_subscribers.ContainsKey(symbol))
            {
                _subscribers[symbol] = new List<Action<PositionUpdate>>();
            }

            _subscribers[symbol].Add(callback);
        }
        #endregion

        #region protected members
        protected override bool StartService()
        {
            _socketService.Subscribe(BitmexSocketSubscriptions.CreatePositionSubsription(message => Post(message.Action, message.Data)));
            return base.StartService();
        }

        protected override bool StopService()
        {
            try
            {
                _socketService.Unsubscribe(BitmexSocketSubscriptions.CreatePositionSubsription(m => { }));
            }
            catch(Exception e)
            {
                _logger.LogInformation(e, "Service stopped; cannot unsubscribe");
            }

            return base.StopService();
        }

        protected override void OnTimer()
        {
            try
            {
                _logger.LogInformation("PMS timer elapsed");
                var positions = GetPositions();
                foreach (var pos in positions)
                {
                    AddOrUpdate(pos);
                }
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex, "Position service on timer failed");
            }
        }
        #endregion

        #region private members
        private IEnumerable<PositionDto> GetPositions()
        {
            try
            {
                _logger.LogInformation("Fetching positions for PMS reconciliation");
                PositionGETRequestParams param = new PositionGETRequestParams();
                var task = _restService.Execute(BitmexApiUrls.Position.GetPosition, param);
                task.Wait(10000);
                if (task.IsCompleted)
                {
                    return task.Result.Result;
                }
                else
                {
                    _logger.LogError("position fetch timedout in 10 seconds");
                    return new List<PositionDto>();
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "position fetch error");
            }

            return new List<PositionDto>();
        }

        private void AddOrUpdate(PositionDto dto)
        {
            bool updated = true;

            _positionsBySymbol.AddOrUpdate(dto.Symbol, dto,
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
                if (_subscribers.ContainsKey(dto.Symbol))
                {
                    PositionUpdate update = new PositionUpdate
                    {
                        symbol = dto.Symbol,
                        currentQty = dto.CurrentQty,
                        avgPrice = dto.AvgEntryPrice.HasValue ? dto.AvgEntryPrice.Value : 0
                    };

                    _subscribers[dto.Symbol].ForEach(action => action(update));
                }
            }
        }

        private void Post(BitmexActions action, IEnumerable<PositionDto> dtos)
        {
            foreach (var dto in dtos)
            {
                _logger.LogInformation(string.Format("On position {0} for {1}", action.ToString(), dto.Symbol));
                if (action == BitmexActions.Delete)
                {
                    _positionsBySymbol.TryRemove(dto.Symbol, out _);
                }
                else if (action == BitmexActions.Insert || action == BitmexActions.Update || action == BitmexActions.Partial)
                {
                    AddOrUpdate(dto);
                }
            }
        }

        private void _socketService_Stopped(object sender, EventArgs e)
        {
            Stop();
        }
        #endregion
    }
}
