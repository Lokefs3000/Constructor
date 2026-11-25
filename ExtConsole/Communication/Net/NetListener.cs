using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ExtConsole.Communication.Net
{
    public sealed class NetListener : IDisposable
    {
        private Socket _listener;

        private MessageSerializer _serializer;
        private MessageDeserializer _deserializer;

        private CancellationTokenSource _netCts;
        private Thread? _netThread;

        private HashSet<ConnectedClient> _connectedClients;
        private ConcurrentQueue<ClientUpdate> _clientUpdates;

        private int _incomingTraffic;
        private int _outgoingTraffic;

        private bool _hasStarted;

        private bool _disposedValue;

        public NetListener(int port = 14000)
        {
            _listener = new Socket(SocketType.Stream, ProtocolType.Tcp) { Blocking = false };

            _serializer = new MessageSerializer();
            _deserializer = new MessageDeserializer();

            _netCts = new CancellationTokenSource();
            _netThread = null;

            _hasStarted = false;

            _connectedClients = new HashSet<ConnectedClient>();
            _clientUpdates = new ConcurrentQueue<ClientUpdate>();

            IPAddress address = IPAddress.Parse("127.0.0.1");
            _listener.Bind(new IPEndPoint(address, port));
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_hasStarted)
                    TryClose();

                if (disposing)
                {
                    foreach (ConnectedClient client in _connectedClients)
                        client.Dispose();
                    _connectedClients.Clear();

                    _listener.Dispose();
                    _netCts.Dispose();
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
            if (_hasStarted)
                return;

            _listener.Listen();
            _hasStarted = true;

            CommLog.Message.Information("Started listening: {address}", _listener.LocalEndPoint);

            if (startExternalThread)
            {
                if (!_netCts.TryReset())
                {
                    _netCts.Dispose();
                    _netCts = new CancellationTokenSource();
                }

                _netThread = new Thread(NetLoop);
                _netThread.Start();
            }
            else
            {
                NetLoop();
            }
        }

        public void TryClose()
        {
            if (!_hasStarted)
                return;

            if (_netThread != null)
            {
                _netCts.Cancel();
                _netThread.Join();
            }

            _listener.Close();
            _hasStarted = false;
        }

        private void NetLoop()
        {
            try
            {
                byte[] bufferData = new byte[1024];

                TryAcceptAsync();

                while (!_netCts.IsCancellationRequested)
                {
                    _incomingTraffic = 0;
                    _outgoingTraffic = 0;
                    
                    foreach (ConnectedClient client in _connectedClients)
                    {
                        try
                        {
                            client.Client.Send(Array.Empty<byte>());
                        }
                        catch (SocketException ex)
                        {
                            // 10035 == WSAEWOULDBLOCK
                            if (!ex.NativeErrorCode.Equals(10035))
                            {
                                _clientUpdates.Enqueue(new ClientUpdate(client, true));
                            }
                        }
                    }

                    _outgoingTraffic = _serializer.SendProcessedMessages();

                    Thread.Sleep(30);
                }

                async void TryAcceptAsync()
                {
                    while (true)
                    {
                        try
                        {
                            Socket client = await _listener.AcceptAsync(_netCts.Token);
                            ConnectedClient connected = new ConnectedClient(client);

                            _connectedClients.Add(connected);
                            _clientUpdates.Enqueue(new ClientUpdate(connected, false));

                            TryReadAsync(connected);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }

                async void TryReadAsync(ConnectedClient client)
                {
                    while (true)
                    {
                        try
                        {
                            int read = await client.Client.ReceiveAsync(bufferData.AsMemory(), _netCts.Token);

                            _deserializer.ProcessBytes(bufferData.AsSpan(0, read));
                            _incomingTraffic += read;

                            if (read < bufferData.Length)
                                _deserializer.HandleRecievedData();
                        }
                        catch (SocketException)
                        {
                            if (!client.Client.Connected)
                            {
                                _clientUpdates.Enqueue(new ClientUpdate(client, true));
                                break;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                CommLog.Net.Information("Network thread cancellation requested");
            }
            catch (Exception ex)
            {
                CommLog.Net.Error(ex, "Network thread crashed!");
            }
        }

        internal bool TryDequeueClientUpdate(out ClientUpdate update)
        {
            bool ret = _clientUpdates.TryDequeue(out update);

            if (update.WasDisconnected)
                _connectedClients.Remove(update.Client);
            return ret;
        }

        internal MessageSerializer Serializer => _serializer;
        internal MessageDeserializer Deserializer => _deserializer;

        internal int IncomingTraffic => _incomingTraffic;
        internal int OutgoingTraffic => _outgoingTraffic;

        public bool IsConnected => false;

        internal readonly record struct ClientUpdate(ConnectedClient Client, bool WasDisconnected);
    }
}
