using ExtConsole.Communication.Messages;
using ExtConsole.Communication.Net;
using ExtConsole.Communication.Serialization;
using Hexa.NET.SDL3;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication
{
    public sealed class ExtConsoleHost : IDisposable
    {
        private NetListener _listener;

        private HashSet<ConnectedClient> _clients;
        private ConnectedClient? _activeClient;

        private bool _disposedValue;

        public ExtConsoleHost(int port = 14000)
        {
            _listener = new NetListener(port);

            _clients = new HashSet<ConnectedClient>();
            _activeClient = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _listener.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void TryStart(bool startExternalThread)
        {
            _listener.TryStart(startExternalThread);
        }

        public void TryClose()
        {
            _listener.TryClose();
        }

        public void PumpEvents()
        {
            while (_listener.TryDequeueClientUpdate(out NetListener.ClientUpdate update))
            {
                if (update.WasDisconnected)
                {
                    ClientDisconnected?.Invoke(update.Client);
                    _clients.Remove(update.Client);

                    if (_activeClient == update.Client)
                        _activeClient = _clients.FirstOrDefault();
                }
                else
                {
                    ClientConnected?.Invoke(update.Client);
                    _clients.Add(update.Client);

                    if (_clients.Count == 1)
                        _activeClient = update.Client;
                }
            }

            while (_listener.Deserializer.TryDequeueQueuedMessage(out QueuedMessage message))
            {
                MessageRecieved?.Invoke(message);
                _listener.Deserializer.ReturnMessageReader(message.Reader);
            }
        }

        public void SendMessage<T>(ConnectedClient client, T message) where T : IMessage
        {
            _listener.Serializer.Send(client, message);
        }

        public IReadOnlySet<ConnectedClient> Clients => _clients;
        public ConnectedClient? ActiveClient { get => _activeClient; set => _activeClient = value; }

        public int IncomingTraffic => _listener.IncomingTraffic;
        public int OutgoingTraffic => _listener.OutgoingTraffic;

        public event Action<QueuedMessage>? MessageRecieved;

        public event Action<ConnectedClient>? ClientConnected;
        public event Action<ConnectedClient>? ClientDisconnected;
    }
}
