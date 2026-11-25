using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary;
using Primary.Threading;

namespace VoxelizationDemo.Runtime
{
    public sealed class RunningCommunication : IDisposable
    {
        private readonly Process _thisProcess;
        private readonly Process _editorProcess;

        private AnonymousPipeClientStream _inStream;
        private AnonymousPipeClientStream _outStream;

        private bool _disposedValue;

        internal RunningCommunication()
        {
            _thisProcess = Process.GetCurrentProcess();
            _editorProcess = Process.GetProcessById((int)AppArguments.GetValueOrDefault("runtime", -1l));

            _inStream = new AnonymousPipeClientStream(PipeDirection.In, AppArguments.GetValueOrDefault("runtime-pipe-in", string.Empty)!);
            _outStream = new AnonymousPipeClientStream(PipeDirection.Out, AppArguments.GetValueOrDefault("runtime-pipe-out", string.Empty)!);

            ThreadHelper.ExecuteOnMainThread(() => _outStream.Write(_thisProcess.Id));
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _inStream.Dispose();
                    _outStream.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
