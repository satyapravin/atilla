using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using ServiceCore;

namespace ServiceTests
{
    public interface ITimerListener { void OnTimer(); }
    public class TestTimerService : BaseTimerService 
    {
        ITimerListener _listener;
        public TestTimerService(string Name, double interval, ITimerListener listener, ILoggerFactory factory):base(Name, interval, factory) 
        {
            _listener = listener;
        }

        protected override void OnTimer()
        {
            _listener.OnTimer();
        }
    }

    public class TestTimerBaseService
    {
        [Test]
        public void TestBasic()
        {
            var listener = new Mock<ITimerListener>();
            var svc = new TestTimerService("testSvc", 2000, listener.Object, new NullLoggerFactory());
            Assert.AreEqual("testSvc", svc.Name);
            Assert.IsFalse(svc.Status);
            listener.Setup(x => x.OnTimer());
            svc.Start();
            Assert.IsTrue(svc.Status);
            Thread.Sleep(3000);
            listener.Verify(x=>x.OnTimer(), Times.Once());
            svc.Stop();
            Assert.IsFalse(svc.Status);
            Thread.Sleep(2000);
            listener.Verify(x=>x.OnTimer(), Times.Once());
        }
    }
}
