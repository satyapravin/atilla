using System;
using System.Collections.Generic;
using System.Text;

namespace StrategyCore
{
    public class InstrumentConfig
    {
        public IDictionary<string, InstrumentDef> InstrumentDefs { get; set; }
    }
}
