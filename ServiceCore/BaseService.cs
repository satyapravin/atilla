using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceCore
{
    public abstract class BaseService : IBaseService
    {
        public event EventHandler Stopped;

        protected string _name;
        protected volatile bool _status;
        protected bool _disposed = false;
        protected ILogger _logger;
        public BaseService(string name, ILoggerFactory factory)
        {
            _name = name;
            _logger = factory.CreateLogger(this.GetType());
        }

        public string Name { get => _name; }
        public bool Status { get => _status; }
        public void Reset()
        {
            if (_status)
            {
                ResetService();
            }
        }
        public void Start()
        {
            if (!_status)
            {
                _status = StartService();
            }
        }
        public void Stop()
        {
            if (_status)
            {
                _status = !StopService();
                if (!_status)
                {
                    Stopped?.Invoke(this, new EventArgs());
                }
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            Stop();
            _disposed = true;
        }

        abstract protected bool StartService();
        abstract protected bool StopService();
        protected void ResetService() 
        { 
            StopService(); 
            StartService(); 
        }
    }
}
