using BitmexCore;
using StrategyCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AtillaCore
{
    public class AtillaConfig
    {
        public InstrumentConfig Instruments { get; set; }
        public BitmexConfig Bitmex { get; set; }
        public double RebalanceInterval { get; set; } 
        public decimal ETHBTCQuantity { get; set; }
    }
}
