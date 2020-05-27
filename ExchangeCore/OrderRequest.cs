using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeCore
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

    public enum OrderSide { BUY, SELL };

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
}
