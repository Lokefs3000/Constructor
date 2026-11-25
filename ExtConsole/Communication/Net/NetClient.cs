using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static ExtConsole.Communication.Net.NetListener;

namespace ExtConsole.Communication.Net
{
    public sealed class NetClient : IDisposable, IDataClient
    {
        private Socket _client;

        private MessageSerializer _serializer;
        private MessageDeserializer _deserializer;

        private CancellationTokenSource _netCts;
        private Thread? _netThread;

        private bool _isConnected;

        private bool _disposedValue;

        internal NetClient()
        {
            _client = new Socket(SocketType.Stream, ProtocolType.Tcp) { Blocking = false };

            _serializer = new MessageSerializer();
            _deserializer = new MessageDeserializer();

            _netCts = new CancellationTokenSource();
            _netThread = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_isConnected)
                        Disconnect();

                    _client.Dispose();
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

        internal bool TryConnect(bool startExternalThread, int port = 14000, TimeSpan timeout = default)
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");

            long start = Stopwatch.GetTimestamp();
            Exception? finalException = null;

            do
            {
                finalException = null;
                try
                {
                    _client.ConnectAsync(address, port)
                        .Wait(timeout - new TimeSpan(Stopwatch.GetTimestamp() - start));
                }
                catch (Exception ex)
                {
                    finalException = ex;
                }

                Thread.Sleep(100);
            } while (Stopwatch.GetElapsedTime(start) < timeout && !_client.Connected);

            if (finalException != null)
            {
                CommLog.Message.Error(finalException, "Exception occured trying to connect!");
                return false;
            }

            _isConnected = true;

            CommLog.Message.Information("Connected to: {address}", _client.LocalEndPoint);

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

            return true;
        }

        internal void Disconnect()
        {
            if (_isConnected)
            {
                if (_netThread != null)
                {
                    _netCts.Cancel();
                    _netThread.Join();
                }

                if (_client.Connected)
                {
                    try
                    {
                        _client.DisconnectAsync(true).AsTask().Wait();
                    }
                    catch (SocketException)
                    {

                    }
                }

                _isConnected = false;
            }
        }

        private void NetLoop()
        {
            try
            {
                byte[] bufferData = new byte[1024];

                TryRecieveAsync();

                while (!_netCts.IsCancellationRequested)
                {
                    try
                    {
                        ThreadUpdate?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        CommLog.Message.Error(ex, "Failed to fire thread update event!");
                    }

                    try
                    {
                        _client.Send(ReadOnlySpan<byte>.Empty);
                    }
                    catch (SocketException ex)
                    {
                        // 10035 == WSAEWOULDBLOCK
                        if (!ex.NativeErrorCode.Equals(10035))
                        {
                            break;
                        }
                    }

                    _serializer.SendProcessedMessages();

                    Thread.Sleep(30);
                }

                async void TryRecieveAsync()
                {
                    while (true)
                    {
                        try
                        {
                            int recieved = await _client.ReceiveAsync(bufferData.AsMemory(), _netCts.Token);

                            _deserializer.ProcessBytes(bufferData.AsSpan(0, recieved));

                            if (recieved < bufferData.Length)
                                _deserializer.HandleRecievedData();
                        }
                        catch (SocketException)
                        {
                            if (!_client.Connected)
                            {
                                _netCts.Cancel();
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
                throw;
            }

            Disconnect();
        }

        public void SendRaw(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
                return;
            _client.Send(data);
        }

        internal MessageSerializer Serializer => _serializer;
        internal MessageDeserializer Deserializer => _deserializer;

        internal event Action? ThreadUpdate;

        public bool IsConnected => _client.Connected;
    }
}
