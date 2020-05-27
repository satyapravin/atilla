using BitmexCore.Dtos;
using ServiceCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeCore
{
    public interface IOMS : IBaseService
    {
        public List<OrderDto> GetOpenOrders(string symbol);
        public void NewOrder(OrderRequest req);
        public void CancelOrder(OrderRequest req);
        public void CancelAll();
        public void Amend(OrderRequest req);
        public void NewOrder(List<OrderRequest> reqs);
        public void Amend(List<OrderRequest> reqs);
    }
}
