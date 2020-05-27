using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace ServiceCore
{
    public abstract class BaseTimerService : BaseService
    {
        protected readonly object _timerLock = new object();
        protected readonly double _interval;
        protected Timer _timer;
        public BaseTimerService(string name, double interval, ILoggerFactory factory) 
                                : base(name, factory) 
        {
            _interval = interval;
        }

         ~BaseTimerService()
        {
            Dispose(false);
        }
        protected override bool StartService()
        {
            lock (_timerLock)
            {
                if (_timer == null)
                {
                    _timer = new Timer(_interval);
                    _timer.Elapsed += _timer_Elapsed;
                    _timer.Enabled = true;
                }
            }

            return true;
        }

        protected override bool StopService()
        {
            lock(_timerLock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            base.Dispose(disposing);
        }

        protected abstract void OnTimer();

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (_timerLock)
                {
                    if (_timer != null)
                    {
                        _timer.Enabled = false;
                        OnTimer();
                    }
                }
            }
            finally { lock (_timerLock) { if (_timer != null) _timer.Enabled = true; } }
        }
    }
}
