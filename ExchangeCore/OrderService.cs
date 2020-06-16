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

namespace ExchangeCore
{
    public class OrderService : BaseTimerService, IOMS
    {
        #region private members
        private readonly IBitmexApiService _restService;
        private readonly WebSocketService _socketService;
        private readonly ConcurrentDictionary<string, OrderCache> _ordersBySymbol = new ConcurrentDictionary<string, OrderCache>();
        private static readonly string _classname = typeof(OrderService).Name;
        #endregion

        #region public members
        public OrderService(IBitmexApiService restSvc,
                                     WebSocketService _socketSvc,
                                     ILoggerFactory factory):base(_classname, 5000, factory)
        {
            _restService = restSvc;
            _socketService = _socketSvc;
            _socketService.Stopped += _socketService_Stopped;
            _socketService.OnClosed += _socketService_OnClosed;
        }

        private void _socketService_OnClosed(object sender, BitmexWebSocket.Models.Socket.Events.BitmexCloseEventArgs e)
        {
            Stop();
        }

        private void _socketService_Stopped(object sender, EventArgs e)
        {
            Stop();
        }

        public List<OrderDto> GetOpenOrders(string symbol)
        {
            if (_status)
            {
                var cache = _ordersBySymbol.GetOrAdd(symbol, new OrderCache());
                return cache.GetOpenOrders();
            }
            else
            {
                throw new ApplicationException("Service not running");
            }
        }

        public void NewOrder(OrderRequest req)
        {
            var order = NewOrderRequestToParam(req);
            try
            {
                var response = _restService.Execute(BitmexApiUrls.Order.PostOrder, order);
                response = TaskExtensions.TimeoutAfter(response, new TimeSpan(0, 0, 5));
                var dto = response.Result.Result;
                var cache = _ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                cache.Reconcile(dto);
            }
            catch (TimeoutException e)
            {
                _logger.LogError(e, "new order request timed out in 5 seconds");
            }
            catch (BitmexApiException e)
            {
                _logger.LogError(e, "new order failed");
                if (e.StatusCode != 503)
                {
                    throw;
                }
            }
            catch (AggregateException e)
            {
                _logger.LogError(e, "new order failed");
                e.Handle(inner =>
                {
                    if (inner is BitmexApiException exception && exception.StatusCode == 503)
                        return true;
                    else
                        return false;
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "new order request failed");
            }
        }

        public void CancelOrder(OrderRequest req)
        {
            OrderDELETERequestParams order = new OrderDELETERequestParams()
            {
                OrderID = req.orderId
            };

            try
            {
                var result = _restService.Execute(BitmexApiUrls.Order.DeleteOrder, order);
                result = TaskExtensions.TimeoutAfter(result, new TimeSpan(0, 0, 5));
            }
            catch (TimeoutException e)
            {
                _logger.LogError(e, "Cancel timeout");
            }
            catch (BitmexApiException e)
            {
                _logger.LogError(e, "Cancel order failed with bitmexException");

                if (e.StatusCode != 503)
                {
                    throw;
                }
            }
            catch (AggregateException e)
            {
                _logger.LogError(e, "cancel order failed");
                e.Handle(inner =>
                {
                    if (inner is BitmexApiException exception && exception.StatusCode == 503)
                        return true;
                    else
                        return false;
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cancel order failed with general exception");
                throw;
            }
        }

        public void CancelAll()
        {
            while (true)
            {
                try
                {
                    OrderAllDELETERequestParams req = new OrderAllDELETERequestParams();
                    var result = _restService.Execute(BitmexApiUrls.Order.DeleteOrderAll, req);
                    result = TaskExtensions.TimeoutAfter(result, new TimeSpan(0, 0, 10));
                    return;
                }
                catch (TimeoutException e)
                {
                    _logger.LogError(e, "CancelAll timed out");
                    return;
                }
                catch (BitmexApiException e)
                {
                    _logger.LogError(e, "CancelAll failed with bitmexException");
                    if (e.StatusCode != 503)
                    {
                        throw;
                    }
                }
                catch (AggregateException e)
                {
                    _logger.LogError(e, "Cancel all failed");
                    e.Handle(inner =>
                    {
                        if (inner is BitmexApiException exception && exception.StatusCode == 503)
                            return true;
                        else
                            return false;
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Cancel all failed");
                    throw;
                }
            }
        }

        public void Amend(OrderRequest req)
        {
            var order = AmendOrderRequestToParam(req);
            try
            {
                var response = _restService.Execute(BitmexApiUrls.Order.PutOrder, order);
                response = TaskExtensions.TimeoutAfter(response, new TimeSpan(0, 0, 5));
                var dto = response.Result.Result;
                var cache = _ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                cache.Reconcile(dto);
            }
            catch (TimeoutException e)
            {
                _logger.LogError(e, "Amend timed out");
            }
            catch (BitmexApiException e)
            {
                _logger.LogError(e, "Amend order failed with bitmexException");
                if (e.StatusCode != 503)
                {
                    throw;
                }
            }
            catch (AggregateException e)
            {
                _logger.LogError(e, "Amend order failed");
                e.Handle(inner =>
                {
                    if (inner is BitmexApiException exception && exception.StatusCode == 503)
                        return true;
                    else
                        return false;
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Amend order failed with general exception");
                throw e;
            }
        }

        public void NewOrder(List<OrderRequest> reqs)
        {
            List<OrderPOSTRequestParams> orders = new List<OrderPOSTRequestParams>();

            foreach (var req in reqs)
            {
                orders.Add(NewOrderRequestToParam(req));
            }

            OrderBulkPOSTRequestParams order = new OrderBulkPOSTRequestParams
            {
                Orders = orders.ToArray()
            };

            try
            {
                var response = _restService.Execute(BitmexApiUrls.Order.PostOrderBulk, order);
                response = TaskExtensions.TimeoutAfter(response, new TimeSpan(0, 0, 5));
                var dtos = response.Result.Result;

                foreach (var dto in dtos)
                {
                    var cache = _ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                    cache.Reconcile(dto);
                }
            }
            catch (TimeoutException e)
            {
                _logger.LogError(e, "NewOrder bulk timed out");
            }
            catch (BitmexApiException e)
            {
                _logger.LogError(e, "New order failed with bitmexException");
                if (e.StatusCode != 503)
                {
                    throw;
                }
            }
            catch (AggregateException e)
            {
                _logger.LogError(e, "new bulk order failed");
                e.Handle(inner =>
                {
                    if (inner is BitmexApiException exception && exception.StatusCode == 503)
                        return true;
                    else
                        return false;
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "New bulk order failed with general exception");
            }
        }

        public void Amend(List<OrderRequest> reqs)
        {
            List<OrderPUTRequestParams> orders = new List<OrderPUTRequestParams>();

            foreach (var req in reqs)
            {
                orders.Add(AmendOrderRequestToParam(req));
            }

            OrderBulkPUTRequestParams order = new OrderBulkPUTRequestParams
            {
                Orders = orders.ToArray()
            };

            try
            {
                var response = _restService.Execute(BitmexApiUrls.Order.PutOrderBulk, order);
                response = TaskExtensions.TimeoutAfter(response, new TimeSpan(0, 0, 5));
                var dtos = response.Result.Result;
                foreach (var dto in dtos)
                {
                    var cache = _ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                    cache.Reconcile(dto);
                }
            }
            catch (TimeoutException e)
            {
                _logger.LogError(e, "Amend bulk order timedout");
            }
            catch (BitmexApiException e)
            {
                _logger.LogError(e, "Amend bulk order failed with bitmexException");
                if (e.StatusCode != 503)
                {
                    throw;
                }
            }
            catch (AggregateException e)
            {
                _logger.LogError(e, "Amend bulk order failed");
                e.Handle(inner =>
                {
                    if (inner is BitmexApiException exception && exception.StatusCode == 503)
                        return true;
                    else
                        return false;
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Amend bulk order failed with general exception");
                throw e;
            }
        }

        public void ClosePosition(string symbol)
        {
            var order = new OrderPOSTRequestParams() { Symbol = symbol, ExecInst = "Close" };
            try
            {
                var response = _restService.Execute(BitmexApiUrls.Order.PostOrder, order);
                response = TaskExtensions.TimeoutAfter(response, new TimeSpan(0, 0, 5));
                var dto = response.Result.Result;
                var cache = _ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                cache.Reconcile(dto);
            }
            catch (TimeoutException e)
            {
                _logger.LogError(e, "close position timed out in 5 seconds");
            }
            catch (BitmexApiException e)
            {
                _logger.LogError(e, "close position order failed");
                if (e.StatusCode != 503)
                {
                    throw;
                }
            }
            catch (AggregateException e)
            {
                _logger.LogError(e, "close position order failed");
                e.Handle(inner =>
                {
                    if (inner is BitmexApiException exception && exception.StatusCode == 503)
                        return true;
                    else
                        return false;
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "new close position request failed");
            }
        }
        #endregion

        #region protected members
        protected override bool StartService()
        {
            _logger.LogInformation("OMS starting and creating order subscription");
            _socketService.Subscribe(BitmexSocketSubscriptions.CreateOrderSubsription(message => Post(message.Action, message.Data)));
            return base.StartService();
        }

        protected override bool StopService()
        {
            try
            {
                _socketService.Unsubscribe(BitmexSocketSubscriptions.CreateOrderSubsription(m => { }));
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
                _logger.LogInformation("OMS reconcilation timer elapsed");
                var openOrders = GetOpenOrders();

                if (openOrders != null)
                {
                    foreach (var order in openOrders)
                    {
                        var cache = _ordersBySymbol.GetOrAdd(order.Symbol, new OrderCache());
                        cache.Reconcile(order);
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex, "OMS OnTimer failed");
            }
        }
        #endregion

        #region private members
        private IEnumerable<OrderDto> GetOpenOrders()
        {
            try
            {
                OrderGETRequestParams param = new OrderGETRequestParams();
                param.Filter = new Dictionary<string, string>();
                param.Filter.Add("open", "true");
                var task = _restService.Execute(BitmexApiUrls.Order.GetOrder, param);
                task.Wait(10000);
                if (task.IsCompleted)
                {
                    return task.Result.Result;
                }
                else
                {
                    _logger.LogError("GetOpenOrders timeout");
                    return new List<OrderDto>();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetOpenOrders failed");
            }

            return new List<OrderDto>();
        }

        private void Post(BitmexActions action, IEnumerable<OrderDto> dtos)
        {
            _logger.LogInformation(string.Format("Order notification received {0}", action.ToString()));

            foreach (var dt in dtos)
            {
                if (!string.IsNullOrEmpty(dt.OrderId))
                {
                    _logger.LogInformation(string.Format("{0}, {1}", dt.OrderId, dt.OrdStatus));
                    var cache = _ordersBySymbol.GetOrAdd(dt.Symbol, new OrderCache());
                    cache.Reconcile(dt);
                }
            }
        }

        private OrderPOSTRequestParams NewOrderRequestToParam(OrderRequest req)
        {
            OrderPOSTRequestParams order = new OrderPOSTRequestParams
            {
                Symbol = req.symbol,
                Side = req.side == OrderSide.BUY ? "Buy" : "Sell",
                OrderQty = req.quantity,
            };

            if (req.orderType == OrderType.NEW_LIMIT_ORDER)
            {
                order.Price = req.price;
                order.OrdType = "Limit";
            }
            else if (req.orderType == OrderType.NEW_MARKET_ORDER)
            {
                order.OrdType = "Market";
            }
            else
            {
                _logger.LogError(string.Format("invalid order type {0}", req.orderType));
                throw new ApplicationException("Invalid order type for new order");
            }

            if (req.isPassive)
            {
                order.ExecInst = "ParticipateDoNotInitiate";
            }

            return order;
        }

        private OrderPUTRequestParams AmendOrderRequestToParam(OrderRequest req)
        {
            OrderPUTRequestParams order = new OrderPUTRequestParams()
            {
                OrderID = req.orderId,
            };

            if (req.orderType == OrderType.AMEND_PRICE_AND_QUANTITY)
            {
                order.Price = req.price;
                order.OrderQty = req.quantity;
            }
            else if (req.orderType == OrderType.AMEND_PRICE_ONLY)
            {
                order.Price = req.price;
            }
            else if (req.orderType == OrderType.AMEND_QUANTITY_ONLY)
            {
                order.OrderQty = req.quantity;
            }
            else
            {
                _logger.LogCritical(string.Format("Invalid order type for amend {0}", req.orderType));
                throw new ApplicationException("Invalid ordertype for Amend");
            }

            return order;
        }
        #endregion
    }
}
