using ServiceCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrategyCore
{
    public class InstrumentDef
    {
        public InstrumentDef(string name, string code, decimal tickSize)
        {
            Name = name; Code = code; TickSize = tickSize;
        }
        public string Name { get; }
        public string Code { get; }
        public decimal TickSize { get; }
    }

    public interface IInstrumentService: IBaseService
    {
        public void Set(IDictionary<string, InstrumentDef> instrMap);
        public InstrumentDef Get(string instr);
    }
}