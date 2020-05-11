using Bitmex.NET.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange
{
    public interface IOMS
    {
        void Start();
        List<OrderDto> GetOpenOrders(string symbol);
        void Stop();
        void NewOrder(OrderRequest req);
        void CancelOrder(OrderRequest req);
        void CancelAll();
        void Amend(OrderRequest req);
        void NewOrder(List<OrderRequest> reqs);
        void Amend(List<OrderRequest> reqs);
    }
}
