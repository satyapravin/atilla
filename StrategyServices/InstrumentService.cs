using Microsoft.Extensions.Logging;
using ServiceCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrategyCore
{
    public class InstrumentService : BaseService, IInstrumentService
    {
        private static Dictionary<string, InstrumentDef> _instrumentDefinitions;
        private static readonly string _className = typeof(InstrumentService).Name;
        public InstrumentService(InstrumentConfig config, 
                                 ILoggerFactory factory) : base(_className, factory) 
        {
            _instrumentDefinitions = new Dictionary<string, InstrumentDef>();
            Set(config.InstrumentDefs);
        }

        public InstrumentDef Get(string instr)
        {
            lock (_instrumentDefinitions)
            {
                return _instrumentDefinitions[instr];
            }
        }

        public void Set(IDictionary<string, InstrumentDef> instrMap)
        {
            lock(_instrumentDefinitions)
            {
                foreach(var instr in instrMap.Keys)
                {
                    _instrumentDefinitions[instr] = instrMap[instr];
                }
            }
        }

        protected override bool StartService()
        {
            return true;
        }

        protected override bool StopService()
        {
            return true;
        }
    }
}