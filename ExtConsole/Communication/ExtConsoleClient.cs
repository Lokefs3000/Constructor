using ExtConsole.Communication.Messages;
using ExtConsole.Communication.Net;
using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication
{
    public sealed class ExtConsoleClient : IDisposable
    {
        private NetClient _client;
        private int _port;

        private bool _disposedValue;

        public ExtConsoleClient(int port = 14000)
        {
            _client = new NetClient();
            _port = port;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool TryConnect(bool startExternalThread, TimeSpan timeout)
        {
            return _client.TryConnect(startExternalThread, _port, timeout);
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        public void PumpEvents()
        {
            while (_client.Deserializer.TryDequeueQueuedMessage(out QueuedMessage message))
            {
                MessageRecieved?.Invoke(message);
                _client.Deserializer.ReturnMessageReader(message.Reader);
            }
        }

        public void SendMessage<T>(T message) where T : IMessage
        {
            _client.Serializer.Send(_client, message);
        }

        public event Action<QueuedMessage>? MessageRecieved;

        public event Action? NetworkThreadUpdate { add => _client.ThreadUpdate += value; remove => _client.ThreadUpdate -= value; }
    }
}
