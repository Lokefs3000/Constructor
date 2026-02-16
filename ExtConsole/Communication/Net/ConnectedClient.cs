using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ExtConsole.Communication.Net
{
    public sealed class ConnectedClient : IDisposable, IDataClient
    {
        private Socket _client;

        private bool _isVerified;

        private bool _disposedValue;

        internal ConnectedClient(Socket client)
        {
            _client = client;

            _isVerified = false;
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

        public void SendRaw(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
                return;
            _client.Send(data);
        }

        internal Socket Client => _client;

        public bool IsConnected => _client.Connected;
    }
}
