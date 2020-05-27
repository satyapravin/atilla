using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using BitmexCore.Dtos;

namespace ExchangeCore
{
    public class OrderCache
    {
        private readonly ConcurrentDictionary<string, OrderDto> _openOrders = new ConcurrentDictionary<string, OrderDto>();
        public OrderCache() { }

        public void Reconcile(OrderDto dto)
        {
            if (IsActive(dto))
            {
                _openOrders.AddOrUpdate(dto.OrderId, dto, 
                    (k, v) =>
                    {
                        if (dto.TransactTime > v.TransactTime)
                        {
                            if (!dto.OrderQty.HasValue)
                                dto.OrderQty = v.OrderQty;

                            if (string.IsNullOrEmpty(dto.Side))
                                dto.Side = v.Side;

                            return dto;
                        }
                        else
                        {
                            return v;
                        }
                    });
            }
            else
            {
                _openOrders.TryRemove(dto.OrderId, out _);
            }
        }

        public List<OrderDto> GetOpenOrders()
        {
            return _openOrders.Values.ToList();
        }

        private bool IsActive(OrderDto dto)
        {
            var status = dto.OrdStatus;
            return status != "Filled" && status != "Canceled" && status != "DoneForDay" && status != "Stopped" && status != "Rejected" && status != "Expired";
        }
    }
}