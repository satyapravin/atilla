using Bitmex.NET.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange
{
    public interface IPMS
    {
        #region public interface
        void Subscribe(string symbol, Action<PositionUpdate> callback);
        void Start();
        void Stop();
        PositionDto GetPosition(string symbol);
        PositionDto QueryPositionFromExchange(string symbol);
        
        #endregion
    }
}
