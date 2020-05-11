using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitmex.NET.Dtos;

namespace Exchange
{
    class OrderCache
    {
        private ConcurrentDictionary<string, OrderDto> openOrders = new ConcurrentDictionary<string, OrderDto>();
        public OrderCache() { }

        public void Reconcile(OrderDto dto)
        {
            if (IsActive(dto))
            {
                openOrders.AddOrUpdate(dto.OrderId, dto, 
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
                openOrders.TryRemove(dto.OrderId, out _);
            }
        }

        public List<OrderDto> GetOpenOrders()
        {
            return openOrders.Values.ToList();
        }

        private bool IsActive(OrderDto dto)
        {
            var status = dto.OrdStatus;
            return status != "Filled" && status != "Canceled" && status != "DoneForDay" && status != "Stopped" && status != "Rejected" && status != "Expired";
        }
    }
}
