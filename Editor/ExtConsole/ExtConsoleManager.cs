using Editor.Platform.Windows;
using ExtConsole.Communication;
using ExtConsole.Communication.Messages;
using ExtConsole.Communication.Messages.General;
using Primary;
using Primary.Common;
using Primary.Profiling;
using Primary.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using ExtConsoleProgram = ExtConsole.Program;

namespace Editor.ExtConsole
{
    internal sealed class ExtConsoleManager : IDisposable
    {
        private Process? _extConsoleProcess;
        private ExtConsoleClient _client;

        private ISubComponent[] _subComponents;
        private int _activeSubComponent;

        private bool _disposedValue;

        internal ExtConsoleManager(StartupDisplayUI startupDisplay)
        {
            startupDisplay.PushStep("Preparing extcon");

            int port = 14000;

            if (AppArguments.HasArgument("--start-extcon"))
            {
                port = FindAvailablePort();

                _extConsoleProcess = Process.Start(new ProcessStartInfo("ExtConsole.exe", $"--host-port {port} --attached")
                {
                    UseShellExecute = true,
                });
                
                if (_extConsoleProcess == null)
                {
                    port = 14000;
                }
            }

            _client = new ExtConsoleClient(port);

            _subComponents = [
                new ProfilerSubComponent()
                ];
            _activeSubComponent = -1;

            _client.NetworkThreadUpdate += NetworkThreadUpdate;
            _client.MessageRecieved += MessageRecieved;

            startupDisplay.PushStep("Connecting to extcon");

            if (!_client.TryConnect(true, TimeSpan.FromSeconds(5.0)))
                EdLog.ExtConsole.Error("Failed to connect to external console");

            startupDisplay.PopStep();
            startupDisplay.PopStep();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _client.Dispose();

                    _client.NetworkThreadUpdate -= NetworkThreadUpdate;
                    _client.MessageRecieved -= MessageRecieved;

                    if (_extConsoleProcess != null)
                    {
                        _extConsoleProcess.Close();
                        _extConsoleProcess = null;
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void PollUpdates()
        {
            using (new ProfilingScope("ExtConsolePoll"))
            {
                _client.PumpEvents();
            }
        }

        private void NetworkThreadUpdate()
        {
            ThreadHelper.ExecuteOnMainThread(() =>
            {
                if (_activeSubComponent != -1)
                {
                    _subComponents[_activeSubComponent].UpdatePackets(_client);
                }
            });
        }

        private void MessageRecieved(QueuedMessage message)
        {
            switch (message.Id)
            {
                case MessageId.ChangeComponent:
                    {
                        MsgChangeComponent msg = message.Deserialize<MsgChangeComponent>();
                        _activeSubComponent = (int)msg.Component;

                        break;
                    }
                default:
                    {
                        if (_activeSubComponent != -1)
                        {
                            _subComponents[_activeSubComponent].RecievePacket(message);
                        }

                        break;
                    }
            }
        }

        private static int FindAvailablePort()
        {
            using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);

            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            listener.Stop();

            return port;
        }
    }
}
