using BitmexCore.Models;
using BitmexWebSocket.Models.Socket;
using BitmexWebSocket.Models.Socket.Events;
using Newtonsoft.Json;
using SuperSocket.ClientEngine;
using System;
using System.Linq;
using System.Threading;
using WebSocket4Net;
using DataEventArgs = BitmexWebSocket.Models.Socket.Events.DataEventArgs;
using Microsoft.Extensions.Logging;
using BitmexCore;
using BitmexWebSocket.Dtos.Socket;

namespace BitmexWebSocket
{
    public interface IBitmexApiSocketProxy : IDisposable
    {
        event SocketDataEventHandler DataReceived;
        event OperationResultEventHandler OperationResultReceived;
        event BitmextErrorEventHandler ErrorReceived;
        event BitmexCloseEventHandler Closed;
        bool Connect();
        bool Disconnect();
        void Send<TMessage>(TMessage message)
            where TMessage : SocketMessage;
        bool IsAlive { get; }
    }

    public class BitmexApiSocketProxy : IBitmexApiSocketProxy
    {
        private readonly ILogger _logger;
        private readonly IWebSocketFactory _socketFactory;
        private const int SocketMessageResponseTimeout = 10000;
        private readonly ManualResetEvent _welcomeReceived = new ManualResetEvent(false);
        private readonly IBitmexAuthorization _bitmexAuthorization;
        public event SocketDataEventHandler DataReceived;
        public event OperationResultEventHandler OperationResultReceived;
        public event BitmextErrorEventHandler ErrorReceived;
        public event BitmexCloseEventHandler Closed;
        private IWebSocket _socketConnection;

        public bool IsAlive => _socketConnection?.State == WebSocketState.Open;

        public BitmexApiSocketProxy(IBitmexAuthorization bitmexAuthorization, IWebSocketFactory socketFactory, ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<BitmexApiSocketProxy>();
            _socketFactory = socketFactory;
            _bitmexAuthorization = bitmexAuthorization;
        }

        public bool Connect()
        {
            CloseConnectionIfItsNotNull();
            _socketConnection = _socketFactory.Create($"wss://{Environments.Values[_bitmexAuthorization.BitmexEnvironment]}/realtime");
            _socketConnection.EnableAutoSendPing = true; 
            _socketConnection.AutoSendPingInterval = 2;
            BitmexWelcomeMessage welcomeData = null;
            void welcomeMessageReceived(object sender, MessageReceivedEventArgs e)
            {
                _logger.LogDebug($"Welcome Data Received {e.Message}");
                welcomeData = JsonConvert.DeserializeObject<BitmexWelcomeMessage>(e.Message);
                _welcomeReceived.Set();
            }
            _socketConnection.MessageReceived += welcomeMessageReceived;
            _socketConnection.Open();
            var waitResult = _welcomeReceived.WaitOne(SocketMessageResponseTimeout);
            _socketConnection.MessageReceived -= welcomeMessageReceived;
            if (waitResult && (welcomeData?.Limit?.Remaining ?? 0) == 0)
            {
                _logger.LogError("Bitmext connection limit reached");
                throw new BitmexWebSocketLimitReachedException();
            }

            if (!waitResult)
            {
                _logger.LogError("Open connection timeout. Welcome message is not received");
                return false;
            }

            if (IsAlive)
            {
                _logger.LogInformation("Bitmex web socket connection opened");
                _socketConnection.MessageReceived += SocketConnectionOnMessageReceived;
                _socketConnection.Closed += SocketConnectionOnClosed;
                _socketConnection.Error += SocketConnectionOnError;
            }

            return IsAlive;
        }

        public bool Disconnect()
        {
            CloseConnectionIfItsNotNull();
            return IsAlive;
        }

        private void CloseConnectionIfItsNotNull()
        {
            if (_socketConnection != null)
            {
                _logger.LogDebug("Closing existing connection");
                using (_socketConnection)
                {
                    _socketConnection.MessageReceived -= SocketConnectionOnMessageReceived;
                    _socketConnection.Closed -= SocketConnectionOnClosed;
                    _socketConnection.Error -= SocketConnectionOnError;
                    _welcomeReceived.Reset();
                    _socketConnection.Close();
                    _socketConnection = null;
                }
            }
        }

        private void SocketConnectionOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _logger.LogDebug($"Message received {e.Message}");
            var operationResult = JsonConvert.DeserializeObject<BitmexSocketOperationResultDto>(e.Message);
            if (operationResult.Request?.Operation != null && (operationResult.Request?.Arguments?.Any() ?? false))
            {
                OnOperationResultReceived(new OperationResultEventArgs(operationResult.Request.Operation.Value, operationResult.Success, operationResult.Error, operationResult.Status, operationResult.Request.Arguments));
                return;
            }

            var data = JsonConvert.DeserializeObject<BitmexSocketDataDto>(e.Message);
            if (!string.IsNullOrWhiteSpace(data.TableName) && (data.AdditionalData?.ContainsKey("data") ?? false))
            {
                OnDataReceived(new DataEventArgs(data.TableName, data.AdditionalData["data"], data.Action));
                return;
            }
        }

        private void SocketConnectionOnError(object sender, ErrorEventArgs e)
        {
            OnErrorReceived(e);
        }


        private void SocketConnectionOnClosed(object sender, EventArgs e)
        {
            OnClosed();
        }

        public void Send<TMessage>(TMessage message)
            where TMessage : SocketMessage
        {
            var json = JsonConvert.SerializeObject(message);
            _logger.LogDebug($"Sending message {json}");
            _socketConnection.Send(json);
        }

        protected virtual void OnDataReceived(DataEventArgs args)
        {
            DataReceived?.Invoke(args);
        }

        protected virtual void OnOperationResultReceived(OperationResultEventArgs args)
        {
            OperationResultReceived?.Invoke(args);
        }

        protected virtual void OnErrorReceived(ErrorEventArgs args)
        {
            _logger.LogError("Socket exception", args.Exception);
            ErrorReceived?.Invoke(new BitmextErrorEventArgs(args.Exception));
        }

        protected virtual void OnClosed()
        {
            _logger.LogDebug("Connection closed");
            Closed?.Invoke(new BitmexCloseEventArgs());
        }

        public void Dispose()
        {
            CloseConnectionIfItsNotNull();
            _welcomeReceived?.Dispose();
            _socketConnection?.Dispose();
            _logger.LogInformation("Disposed...");
        }
    }
}
