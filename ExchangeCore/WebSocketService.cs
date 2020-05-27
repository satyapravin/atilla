using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitmexCore;
using BitmexWebSocket;
using BitmexWebSocket.Models.Socket;
using Microsoft.Extensions.Logging;
using ServiceCore;
using BitmexWebSocket.Models.Socket.Events;

namespace ExchangeCore
{
    public class WebSocketService : BaseService, IBitmexApiSocketService
    {
        private readonly IBitmexApiSocketService _service;
        private static readonly string _classname = typeof(WebSocketService).Name;

        public event EventHandler<BitmexCloseEventArgs> OnClosed;
        public event EventHandler<BitmextErrorEventArgs> OnErrorReceived;

        public WebSocketService(BitmexAuthorization authorization, IWebSocketFactory socketFactory, ILoggerFactory factory):base(_classname, factory)
        {
            _service = BitmexApiSocketService.CreateDefaultApi(authorization, socketFactory, factory);
            _service.OnClosed += OnSocketServiceClosed;
            _service.OnErrorReceived += OnSocketErrorReceived;
        }

        private void OnSocketErrorReceived(object sender, BitmextErrorEventArgs e)
        {
            OnErrorReceived?.Invoke(this, e);
        }

        private void OnSocketServiceClosed(object sender, BitmexCloseEventArgs e)
        {
            OnClosed?.Invoke(this, e);
        }

        public bool Connect(bool authorize)
        {
            return _service.Connect(authorize);
        }

        public bool Disconnect()
        {
            return _service.Disconnect();
        }

        public void Subscribe(BitmexApiSubscriptionInfo subscription)
        {
            if (!_status)
            {
                throw new ApplicationException("service not running");
            }

            _service.Subscribe(subscription);
        }

        public void Unsubscribe(BitmexApiSubscriptionInfo subscription)
        {
            _service.Unsubscribe(subscription);
        }

        public Task UnsubscribeAsync(BitmexApiSubscriptionInfo subscription)
        {
            return _service.UnsubscribeAsync(subscription);
        }

        protected override bool StartService()
        {
            Connect(true);
            return true;
        }

        protected override bool StopService()
        {
            Disconnect();
            return false;
        }
    }
}
