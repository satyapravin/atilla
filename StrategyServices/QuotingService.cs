﻿using BitmexCore.Dtos;
using Microsoft.Extensions.Logging;
using ServiceCore;
using System;
using System.Collections.Generic;
using System.Timers;

namespace ExchangeCore
{
    public class QuotingService : BaseTimerService, IQuotingService
    {
        #region private members
        private volatile bool _set = false;
        private decimal _bidQuantity = 0;
        private decimal _askQuantity = 0;
        private decimal _bidSpreadFromMid = 0;
        private decimal _askSpreadFromMid = 0;
        private decimal _bidPriceRef = 0;
        private decimal _askPriceRef = 0;
        private decimal _tickSize = 0;
        
        private readonly object _lock = new object();
        private readonly string _symbol;
        private readonly IMDS _mdsService;
        private readonly IOMS _omsService;
        private readonly IPMS _pmsService;
        private static readonly string _classname = typeof(QuotingService).Name;
        #endregion
        
        #region public members
        public QuotingService(string sym, IMDS mds, IPMS pms, 
                              IOMS oms, ILoggerFactory factory):base(_classname, 2000, factory)
        {
            _symbol = sym;
            _mdsService = mds;
            _pmsService = pms;
            _omsService = oms;
            _mdsService.Stopped += _mdsService_Stopped;
            _pmsService.Stopped += _pmsService_Stopped;
            _omsService.Stopped += _omsService_Stopped;
        }


        public void SetQuote(decimal bidQty, decimal bidPrice, decimal bidSpread, decimal askQty, decimal askPrice, decimal askSpread, decimal tickSize)
        {
            _set = false;
            lock (_lock)
            {
                _bidQuantity = bidQty;
                _askQuantity = askQty;
                _bidPriceRef = bidPrice;
                _askPriceRef = askPrice;
                _bidSpreadFromMid = bidSpread;
                _askSpreadFromMid = askSpread;
                _tickSize = tickSize;
            }

            _set = true;
        }
        #endregion

        #region protected members
        protected override bool StartService()
        {
            return base.StartService();
        }
        protected override bool StopService()
        {
            _set = false;
            return base.StopService();
        }

        protected override void OnTimer()
        {
            if (_set)
            {
                try
                {
                    decimal bidSize;
                    decimal askSize;
                    decimal bidSpread;
                    decimal askSpread;
                    decimal bidRef;
                    decimal askRef;

                    lock (_lock)
                    {
                        bidSize = _bidQuantity;
                        askSize = _askQuantity;
                        bidSpread = _bidSpreadFromMid;
                        askSpread = _askSpreadFromMid;
                        bidRef = _bidPriceRef;
                        askRef = _askPriceRef;
                    }

                    decimal bidPrice = bidRef - bidRef * bidSpread;
                    bidPrice = bidPrice - bidPrice % _tickSize + ((bidPrice % _tickSize < _tickSize / 2) ? 0.0m : _tickSize);
                    decimal askPrice = askRef + askRef * askSpread;
                    askPrice = askPrice - askPrice % _tickSize + ((askPrice % _tickSize < _tickSize / 2) ? 0.0m : _tickSize);

                    _logger.LogInformation(string.Format("Computed Bid/Ask={0}/{1}", bidPrice, askPrice));
                    var bidAsk = _mdsService.GetBidAsk(_symbol);

                    bool limitSell = false;
                    if (bidAsk.Item1 < bidPrice)
                        bidPrice = bidAsk.Item1;

                    if (bidAsk.Item2 > askPrice)
                    {
                        askPrice = bidAsk.Item2;
                    }

                    _logger.LogInformation(string.Format("Quoting Bid/Ask={0}/{1}", bidPrice, askPrice));
                    var orders = _omsService.GetOpenOrders(_symbol);

                    OrderDto buyOrder = null;
                    OrderDto sellOrder = null;

                    if (orders.Count > 2)
                    {
                        throw new ApplicationException("More than two quote orders found");
                    }
                    else if (orders.Count == 2)
                    {
                        if (orders[0].Side == orders[1].Side)
                            throw new ApplicationException("Both orders have same side");
                    }
                    foreach (var o in orders)
                    {
                        if (o.Side == "Buy")
                            buyOrder = o;
                        else if (o.Side == "Sell")
                            sellOrder = o;
                    }

                    if (buyOrder != null && bidSize == 0)
                    {
                        _logger.LogInformation("Bid target is zero");
                        CancelOrder(buyOrder);
                        buyOrder = null;
                    }

                    if (sellOrder != null && askSize == 0)
                    {
                        _logger.LogInformation("Ask target is zero");
                        CancelOrder(sellOrder);
                        sellOrder = null;
                    }

                    if (buyOrder != null && sellOrder != null)
                    {
                        _logger.LogInformation("Update bid ask quote");
                        Amend(buyOrder, bidSize, bidPrice, sellOrder, askSize, askPrice);
                    }
                    else if (buyOrder != null)
                    {
                        _logger.LogInformation("update bid quote");
                        Amend(buyOrder, bidSize, bidPrice);

                        if (askSize > 0)
                        {
                            _logger.LogInformation("place ask quote");
                            PlaceSingleQuote(askSize, askPrice, OrderSide.SELL);
                        }
                    }
                    else if (sellOrder != null)
                    {
                        _logger.LogInformation("Update ask quote");
                        Amend(sellOrder, askSize, askPrice);

                        if (bidSize > 0)
                        {
                            _logger.LogInformation("place bid quote");
                            PlaceSingleQuote(bidSize, bidPrice, OrderSide.BUY);
                        }
                    }
                    else if (bidSize > 0 && askSize > 0)
                    {
                        _logger.LogInformation("No orders found place quotes");
                        PlaceQuote(bidSize, bidPrice, askSize, askPrice, limitSell);
                    }
                    else if (bidSize > 0)
                    {
                        _logger.LogInformation("No orders found, place only bid quote");
                        PlaceSingleQuote(bidSize, bidPrice, OrderSide.BUY);
                    }
                    else if (askSize > 0)
                    {
                        _logger.LogInformation("No orders found, place only ask quote");
                        PlaceSingleQuote(askSize, askPrice, OrderSide.SELL);
                    }
                }

                catch (Exception ex)
                {
                    _logger.LogCritical("Quote failed", ex);
                    CancelAllOrders();
                }
            }            
        }
        #endregion

        #region private members
        private void _omsService_Stopped(object sender, EventArgs e)
        {
            Stop();
        }

        private void _pmsService_Stopped(object sender, EventArgs e)
        {
            Stop();
        }

        private void _mdsService_Stopped(object sender, EventArgs e)
        {
            Stop();
        }
        private OrderType GetAmendType(OrderDto order, decimal quantity, decimal price)
        {
            if (order.OrdStatus == "Filled" || order.OrdStatus == "Canceled")
            {
                return OrderType.NONE;
            }

            if (order.Price == price && order.OrderQty == quantity)
            {
                return OrderType.NONE;
            }
            else if (order.OrderQty == quantity)
            {
                return OrderType.AMEND_PRICE_ONLY;
            }
            else if (order.Price == price)
            {
                return OrderType.AMEND_QUANTITY_ONLY;
            }
            else
            {
                return OrderType.AMEND_PRICE_AND_QUANTITY;
            }
        }

        private OrderRequest CreateAmendRequest(string ordId, decimal quantity, decimal price, OrderType orderType)
        {
            return new OrderRequest()
            {
                orderId = ordId,
                price = price,
                quantity = quantity,
                isPassive = true,
                orderType = orderType
            };
        }

        private OrderRequest CreateNewRequest(string symbol, decimal q, decimal p, OrderSide side, bool active = false)
        {
            return new OrderRequest()
            {
                symbol = symbol,
                quantity = q,
                price = p,
                side = side,
                isPassive = !active,
                orderType = OrderType.NEW_LIMIT_ORDER
            };
        }

        private void PlaceQuote(decimal buyQ, decimal buyP, decimal sellQ, decimal sellP, bool limitSell)
        {
            var reqs = new List<OrderRequest>();
            reqs.Add(CreateNewRequest(_symbol, buyQ, buyP, OrderSide.BUY));
            reqs.Add(CreateNewRequest(_symbol, sellQ, sellP, OrderSide.SELL, limitSell));
            _logger.LogInformation(string.Format("Quoter quoting at bid {0} {1}, ask {2} {3}", buyQ, buyP, sellQ, sellP));
            _omsService.NewOrder(reqs);
        }

        private void PlaceSingleQuote(decimal q, decimal p, OrderSide side)
        {
            _logger.LogInformation(string.Format("Quoting {0} at {1}, {2}", side == OrderSide.BUY ? "Bid" : "Ask", q, p));
            _omsService.NewOrder(CreateNewRequest(_symbol, q, p, side));
        }

        private void CancelOrder(OrderDto order)
        {
            _logger.LogInformation(string.Format("Canceling quote - {0}", order.OrderId));
            _omsService.CancelOrder(new OrderRequest()
            { orderId = order.OrderId, orderType = OrderType.CANCEL_ORDER });
        }

        private void Amend(OrderDto order, decimal q, decimal p)
        {
            var amend_type = GetAmendType(order, q, p);

            if (amend_type == OrderType.NONE)
            {
                return;
            }

            _logger.LogInformation(string.Format("Amend quote {0}, {1}, {2}", order.OrderId, q, p));
            _omsService.Amend(CreateAmendRequest(order.OrderId, q, p, amend_type));
        }

        private void Amend(OrderDto buyOrder, decimal buyQ, decimal buyP, OrderDto sellOrder, decimal sellQ, decimal sellP)
        {
            var buy_type = GetAmendType(buyOrder, buyQ, buyP);
            var sell_type = GetAmendType(sellOrder, sellQ, sellP);

            List<OrderRequest> reqs = new List<OrderRequest>();

            if (buy_type != OrderType.NONE)
            {
                reqs.Add(CreateAmendRequest(buyOrder.OrderId, buyQ, buyP, buy_type));
            }

            if (sell_type != OrderType.NONE)
            {
                reqs.Add(CreateAmendRequest(sellOrder.OrderId, sellQ, sellP, sell_type));
            }

            if (reqs.Count > 0)
            {
                foreach (var req in reqs)
                {
                    _logger.LogInformation(string.Format("quote for {0} updated to {1} {2}", req.orderId,
                                            req.quantity, req.price));
                }

                _omsService.Amend(reqs);
            }
        }

        private void CancelAllOrders()
        {
            try
            {
                _omsService.CancelAll();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Cancel all failed", ex);
            }
        }        
        #endregion
    }
}