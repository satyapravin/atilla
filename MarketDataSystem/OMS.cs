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
    public enum OrderType 
    { 
        NONE,
        NEW_MARKET_ORDER, 
        NEW_LIMIT_ORDER,
        CANCEL_ORDER,
        AMEND_PRICE_ONLY, 
        AMEND_QUANTITY_ONLY, 
        AMEND_PRICE_AND_QUANTITY 
    };
    
    public enum OrderSide {  BUY, SELL };

    public struct OrderRequest
    {
        public string symbol;
        public string orderId;
        public decimal quantity;
        public decimal price;
        public OrderType orderType;
        public OrderSide side;
        public bool isPassive;
    }

    public class OMS
    {
        private readonly IBitmexApiService restService;
        private readonly IBitmexApiSocketService socketService;
        private Timer timer;
        private ConcurrentDictionary<string, OrderCache> ordersBySymbol = new ConcurrentDictionary<string, OrderCache>();
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(OMS));

        public OMS(IBitmexApiSocketService socketSvc, IBitmexApiService restSvc)
        {
            socketService = socketSvc;
            restService = restSvc;
        }

        public void Start()
        {
            log.Info("OMS starting and creating order subscription");
            socketService.Subscribe(BitmexSocketSubscriptions.CreateOrderSubsription(message => Post(message.Action, message.Data)));
            timer = new Timer(new TimerCallback(OnElapsed), null, 0, 10000);
        }

        public List<OrderDto> GetOpenOrders(string symbol)
        {
            var cache = ordersBySymbol.GetOrAdd(symbol, new OrderCache());
            return cache.GetOpenOrders();
        }

        public void Stop()
        {
            timer.Dispose();
            log.Info("OMS stopped");
        }

        private OrderPOSTRequestParams newOrderRequestToParam(OrderRequest req)
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
                log.Error(string.Format("invalid order type {0}", req.orderType));
                throw new ApplicationException("Invalid order type for new order");
            }

            if (req.isPassive)
            {
                order.ExecInst = "ParticipateDoNotInitiate";
            }

            return order;
        }

        public void NewOrder(OrderRequest req)
        {
            var order = newOrderRequestToParam(req);
            try
            {
                var response = restService.Execute(BitmexApiUrls.Order.PostOrder, order);
                response.Wait(5000);

                if (response.IsCompleted)
                {
                    var dto = response.Result.Result;
                    var cache = ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                    cache.Reconcile(dto);
                }
                else
                {
                    log.Error("new order request timed out in 5 seconds");
                }
            }
            catch(Exception e)
            {
                log.Error("new order request failed", e);
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
                restService.Execute(BitmexApiUrls.Order.DeleteOrder, order);
            }
            catch(Exception e)
            {
                log.Error("Cancel order failed", e);
            }
        }

        private OrderPUTRequestParams amendOrderRequestToParam(OrderRequest req)
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
                log.Fatal(string.Format("Invalid order type for amend {0}", req.orderType));
                throw new ApplicationException("Invalid ordertype for Amend");
            }

            return order;
        }
        public void Amend(OrderRequest req)
        {
            var order = amendOrderRequestToParam(req);
            try
            {
                var response = restService.Execute(BitmexApiUrls.Order.PutOrder, order);
                response.Wait(5000);

                if (response.IsCompleted)
                {
                    var dto = response.Result.Result;
                    var cache = ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                    cache.Reconcile(dto);
                }
                else
                {
                    log.Error("Amend order timed out");
                }
            }
            catch(Exception)
            {
                log.Error("Amend order failed");
            }
        }

        public void NewOrder(List<OrderRequest> reqs)
        {
            List<OrderPOSTRequestParams> orders = new List<OrderPOSTRequestParams>();
            
            foreach(var req in reqs)
            {
                orders.Add(newOrderRequestToParam(req));
            }

            OrderBulkPOSTRequestParams order = new OrderBulkPOSTRequestParams();
            order.Orders = orders.ToArray();

            try
            {
                var response = restService.Execute(BitmexApiUrls.Order.PostOrderBulk, order);
                response.Wait(5000);

                if (response.IsCompleted)
                {
                    var dtos = response.Result.Result;
                    
                    foreach(var dto in dtos)
                    {
                        var cache = ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                        cache.Reconcile(dto);
                    }
                }
                else
                {
                    log.Error("new bulk order timed out");
                }
            }
            catch(Exception e)
            {
                log.Error("New bulk order failed", e);
            }
        }

        public void Amend(List<OrderRequest> reqs)
        {
            List<OrderPUTRequestParams> orders = new List<OrderPUTRequestParams>();

            foreach(var req in reqs)
            {
                orders.Add(amendOrderRequestToParam(req));
            }

            OrderBulkPUTRequestParams order = new OrderBulkPUTRequestParams();
            order.Orders = orders.ToArray();

            try
            {
                var response = restService.Execute(BitmexApiUrls.Order.PutOrderBulk, order);
                response.Wait(5000);
                if (response.IsCompleted)
                {
                    var dtos = response.Result.Result;
                    foreach(var dto in dtos)
                    {
                        var cache = ordersBySymbol.GetOrAdd(dto.Symbol, new OrderCache());
                        cache.Reconcile(dto);
                    }
                }
                else
                {
                    log.Error("Amend bulk order timeout");
                }
            }
            catch(Exception e)
            {
                log.Error("Amend bulk order failed", e);
            }
        }

        private void OnElapsed(object state)
        {
            log.Info("OMS reconcilation timer elapsed");
            var openOrders = GetOpenOrders();

            if (openOrders != null)
            {
                foreach (var order in openOrders)
                {
                    var cache = ordersBySymbol.GetOrAdd(order.Symbol, new OrderCache());
                    cache.Reconcile(order);
                }
            }
        }

        private IEnumerable<OrderDto> GetOpenOrders()
        {
            try
            {
                OrderGETRequestParams param = new OrderGETRequestParams();
                param.Filter = new Dictionary<string, string>();
                param.Filter.Add("open", "true");
                var task = restService.Execute(BitmexApiUrls.Order.GetPosition, param);
                task.Wait(10000);
                if (task.IsCompleted)
                {
                    return task.Result.Result;
                }
                else
                {
                    log.Error("GetOpenOrders timeout");
                    return new List<OrderDto>();
                }
            }
            catch(Exception e)
            {
                log.Error("GetOpenOrders failed", e);
            }

            return new List<OrderDto>();
        }

        private void Post(BitmexActions action, IEnumerable<OrderDto> dtos)
        {
            log.Info(string.Format("Order notification received {0}", action.ToString()));

            foreach(var dt in dtos)
            {
                if (!string.IsNullOrEmpty(dt.OrderId))
                {
                    log.Info(dt.OrderId);
                    var cache = ordersBySymbol.GetOrAdd(dt.Symbol, new OrderCache());
                    cache.Reconcile(dt);
                }
            }
        }
    }
}
