using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ServiceCore;

namespace ServiceTests
{
    public interface  IListener
    {
        bool OnStart();
        bool OnStop();
    }

    public class TestBaseService : BaseService
    {
        private IListener _listener;
        public TestBaseService(string Name, IListener listener) : base(Name, new NullLoggerFactory()) 
        {
            _listener = listener;
        }
        
        protected override bool StartService()
        {
            return _listener.OnStart();
        }

        protected override bool StopService()
        {
            return _listener.OnStop();
        }
    }

    public class ServiceBaseTests
    {
        
        [Test]
        public void TestServiceBaseCreation()
        {
            var _listener = Mock.Of<IListener>(MockBehavior.Strict);
            var svc = new TestBaseService("testsvc", _listener);
            Assert.AreEqual("testsvc", svc.Name);
            Assert.IsFalse(svc.Status);
        }

        [Test]
        public void TestServiceStartStopSuccess()
        {
            var _listener = new Mock<IListener>();
            var svc = new TestBaseService("t", _listener.Object);
            _listener.Setup(x => x.OnStart()).Returns(true);
            _listener.Setup(x => x.OnStop()).Returns(true);
            int eventCalled = 0;
            svc.Stopped += new System.EventHandler((x, e) => { eventCalled++; });
            svc.Start();
            _listener.Verify(x => x.OnStart(), Times.Once);
            Assert.IsTrue(svc.Status);
            svc.Start();
            _listener.Verify(x => x.OnStart(), Times.Once);
            Assert.IsTrue(svc.Status);
            svc.Stop();
            Assert.IsFalse(svc.Status);
            _listener.Verify(x => x.OnStop(), Times.Once);
            svc.Stop();
            Assert.IsFalse(svc.Status);
            Assert.AreEqual(1, eventCalled);
            _listener.Verify(x => x.OnStop(), Times.Once);
            Assert.AreEqual(1, eventCalled);
        }

        [Test]
        public void TestServiceStartStopFailure()
        {
            var _listener = new Mock<IListener>();
            var svc = new TestBaseService("t", _listener.Object);
            _listener.Setup(x => x.OnStart()).Returns(false);
            _listener.Setup(x => x.OnStop()).Returns(false);
            int eventCalled = 0;
            svc.Stopped += new System.EventHandler((x, e) => { eventCalled++; });
            svc.Start();
            _listener.Verify(x => x.OnStart(), Times.Once);
            Assert.IsFalse(svc.Status);
            svc.Start();
            _listener.Verify(x => x.OnStart(), Times.AtLeast(2));
            Assert.IsFalse(svc.Status);
            svc.Stop();
            Assert.IsFalse(svc.Status);
            _listener.Verify(x => x.OnStop(), Times.Never);
            svc.Stop();
            Assert.IsFalse(svc.Status);
            Assert.AreEqual(0, eventCalled);
            _listener.Verify(x => x.OnStop(), Times.Never);
            Assert.AreEqual(0, eventCalled);
        }

        [Test]
        public void TestServiceStartStopPartialFailure()
        {
            var _listener = new Mock<IListener>();
            var svc = new TestBaseService("t", _listener.Object);
            _listener.Setup(x => x.OnStart()).Returns(true);
            _listener.Setup(x => x.OnStop()).Returns(false);
            int eventCalled = 0;
            svc.Stopped += new System.EventHandler((x, e) => { eventCalled++; });
            svc.Start();
            _listener.Verify(x => x.OnStart(), Times.Once);
            Assert.IsTrue(svc.Status);
            svc.Start();
            _listener.Verify(x => x.OnStart(), Times.Once);
            Assert.IsTrue(svc.Status);
            svc.Stop();
            Assert.IsTrue(svc.Status);
            _listener.Verify(x => x.OnStop(), Times.Once);
            svc.Stop();
            Assert.IsTrue(svc.Status);
            Assert.AreEqual(0, eventCalled);
            _listener.Verify(x => x.OnStop(), Times.AtLeast(2));
            Assert.AreEqual(0, eventCalled);
        }
    }
}