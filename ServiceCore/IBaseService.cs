using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceCore
{
    public interface IBaseService : IDisposable
    {
        public event EventHandler Stopped;

        public string Name { get; }
        public bool Status { get; }
        public void Start();
        public void Reset();
        public void Stop();
    }
}
