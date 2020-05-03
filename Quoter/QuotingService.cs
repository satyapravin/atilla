using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Bitmex.NET.Dtos;
using Exchange;

namespace Quoter
{
    public class QuotingService
    {
        private BitmexExchange exchange;
        private string symbol;
        private volatile bool stop = false;
        private Thread backGroundThread;
        private volatile bool isSet = false;
        private decimal bidQuantity = 0;
        private decimal askQuantity = 0;
        private decimal bidSpreadFromMid = 0;
        private decimal askSpreadFromMid = 0;
        private decimal bidPriceRef = 0;
        private decimal askPriceRef = 0;
        private decimal decimalRounder = 0;
        private object paramsLock = new object();
        log4net.ILog log = log4net.LogManager.GetLogger(typeof(QuotingService));

        public QuotingService(BitmexExchange exch, string sym, decimal rounder)
        {
            exchange = exch;
            symbol = sym;
            decimalRounder = 1.0m;
            
            for(int ii =0; ii < rounder; ii++)
                decimalRounder *= 10.0m;

            log.Info(string.Format("Quoter created with rounder {0}", decimalRounder));
        }

        public void SetQuote(decimal bidQ, decimal askQ, decimal bidSpread, decimal askSpread, decimal bidP, decimal askP)
        {
            lock(paramsLock)
            {
                log.Info(string.Format("Quoter set to bid {0}, ask {1}, bidSpread {2}, askSpread {3}, bidRef {4}, askRef {5}",
                          bidQ, askQ, bidSpread, askSpread, bidP, askP));
                bidQuantity = bidQ;
                askQuantity = askQ;
                bidSpreadFromMid = bidSpread;
                askSpreadFromMid = askSpread;
                bidPriceRef = bidP;
                askPriceRef = askP;
            }

            isSet = true;
        }

        public void Start()
        {
            backGroundThread = new Thread(new ThreadStart(Quote));
            backGroundThread.Start();
            log.Info("Quoting service started");
        }

        public void Stop()
        {
            stop = true;
            log.Info("Quoting service stopping");
            backGroundThread.Join();
            log.Info("Quoting service stopped");
        }

        private Exchange.OrderType GetAmendType(OrderDto order, decimal quantity, decimal price)
        {
            if (order.OrdStatus == "Filled" || order.OrdStatus == "Canceled")
            {
                return Exchange.OrderType.NONE;
            }

            if (order.Price == price && order.OrderQty == quantity)
            {
                return Exchange.OrderType.NONE;
            }
            else if (order.OrderQty == quantity)
            {
                return Exchange.OrderType.AMEND_PRICE_ONLY;
            }
            else if (order.Price == price)
            {
                return Exchange.OrderType.AMEND_QUANTITY_ONLY;
            }
            else
            {
                return Exchange.OrderType.AMEND_PRICE_AND_QUANTITY;
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

        private OrderRequest CreateNewRequest(string symbol, decimal q, decimal p, OrderSide side)
        {
            return new OrderRequest()
            {
                symbol = symbol,
                quantity = q,
                price = p,
                side = side,
                isPassive = true,
                orderType = OrderType.NEW_LIMIT_ORDER
            };
        }

        private void PlaceQuote(decimal buyQ, decimal buyP, decimal sellQ, decimal sellP)
        {
            var reqs = new List<OrderRequest>();
            reqs.Add(CreateNewRequest(symbol, buyQ, buyP, OrderSide.BUY));
            reqs.Add(CreateNewRequest(symbol, sellQ, sellP, OrderSide.SELL));
            log.Info(string.Format("Quoter quoting at bid {0} {1}, ask {2} {3}", buyQ, buyP, sellQ, sellP));
            exchange.OrderSystem.NewOrder(reqs);
        }

        private void PlaceSingleQuote(decimal q, decimal p, OrderSide side)
        {
            log.Info(string.Format("Quoting {0} at {1}, {2}", side == OrderSide.BUY ? "Bid" : "Ask", q, p));
            exchange.OrderSystem.NewOrder(CreateNewRequest(symbol, q, p, side));
        }

        private void CancelOrder(OrderDto order)
        {
            log.Info(string.Format("Canceling quote - {0}", order.OrderId));
            exchange.OrderSystem.CancelOrder(new OrderRequest()
            { orderId = order.OrderId, orderType = OrderType.CANCEL_ORDER });
        }

        private void Amend(OrderDto order, decimal q, decimal p)
        {
            var amend_type = GetAmendType(order, q, p);

            if (amend_type == OrderType.NONE)
            {
                return;
            }

            log.Info(string.Format("Amend quote {0}, {1}, {2}", order.OrderId, q, p));
            exchange.OrderSystem.Amend(CreateAmendRequest(order.OrderId, q, p, amend_type));
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
                    log.Info(string.Format("quote updated to {0} {1} {2}", req.side == OrderSide.BUY ? "Bid" : "Ask", 
                                            req.quantity, req.price));
                }

                exchange.OrderSystem.Amend(reqs);
            }
        }
        private void Quote()
        {
            while(!stop)
            {
                try
                {
                    if (isSet)
                    {
                        decimal bidSize;
                        decimal askSize;
                        decimal bidSpread;
                        decimal askSpread;
                        decimal bidRef;
                        decimal askRef;

                        lock (paramsLock)
                        {
                            bidSize = bidQuantity;
                            askSize = askQuantity;
                            bidSpread = bidSpreadFromMid;
                            askSpread = askSpreadFromMid;
                            bidRef = bidPriceRef;
                            askRef = askPriceRef;
                        }

                        decimal bidPrice = decimal.Floor((bidRef - bidRef * bidSpreadFromMid) * decimalRounder) / decimalRounder;
                        decimal askPrice = decimal.Ceiling((askRef + askRef * askSpreadFromMid) * decimalRounder) / decimalRounder;
                        var bidAsk = exchange.MarketDataSystem.GetBidAsk(symbol);

                        if (bidAsk.Item1 < bidPrice)
                            bidPrice = bidAsk.Item2 - 1.0m / decimalRounder;

                        if (bidAsk.Item2 > askPrice)
                            askPrice = bidAsk.Item1 + 1.0m / decimalRounder;

                        var orders = exchange.OrderSystem.GetOpenOrders(symbol);

                        OrderDto buyOrder = null;
                        OrderDto sellOrder = null;

                        if (orders.Count() > 2)
                        {
                            throw new ApplicationException("More than two quote orders found");
                        }

                        foreach (var o in orders)
                        {
                            if (o.Side == "Buy")
                                buyOrder = o;
                            else
                                sellOrder = o;
                        }

                        if (buyOrder != null && bidSize == 0)
                        {
                            log.Info("Bid target is zero");
                            CancelOrder(buyOrder);
                            buyOrder = null;
                        }

                        if (sellOrder != null && askSize == 0)
                        {
                            log.Info("Ask target is zero");
                            CancelOrder(sellOrder);
                            sellOrder = null;
                        }

                        if (buyOrder != null && sellOrder != null)
                        {
                            log.Info("Update bid ask quote");
                            Amend(buyOrder, bidSize, bidPrice, sellOrder, askSize, askPrice);
                        }
                        else if (buyOrder != null)
                        {
                            log.Info("update bid quote");
                            Amend(buyOrder, bidSize, bidPrice);

                            if (askSize > 0)
                            {
                                log.Info("place ask quote");
                                PlaceSingleQuote(askSize, askPrice, OrderSide.SELL);
                            }
                        }
                        else if (sellOrder != null)
                        {
                            log.Info("Update ask quote");
                            Amend(sellOrder, askSize, askPrice);

                            if (bidSize > 0)
                            {
                                log.Info("place bid quote");
                                PlaceSingleQuote(bidSize, bidPrice, OrderSide.BUY);
                            }
                        }
                        else if (bidSize > 0 && askSize > 0)
                        {
                            log.Info("No orders found place quotes");
                            PlaceQuote(bidSize, bidPrice, askSize, askPrice);
                        }
                        else if (bidSize > 0)
                        {
                            log.Info("No orders found, place only bid quote");
                            PlaceSingleQuote(bidSize, bidPrice, OrderSide.BUY);
                        }
                        else if (askSize > 0)
                        {
                            log.Info("No orders found, place only ask quote");
                            PlaceSingleQuote(askSize, askPrice, OrderSide.SELL);
                        }
                    }
                }
                catch(Exception e)
                {
                    log.Fatal("Quoter died", e);
                    stop = true;
                }

                Thread.Sleep(3000);
            }
        }
    }
}
