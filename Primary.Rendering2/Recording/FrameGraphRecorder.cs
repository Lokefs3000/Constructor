using Primary.Pooling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering2.Recording
{
    public sealed class FrameGraphRecorder : IDisposable
    {
        private DisposableObjectPool<CommandRecorder> _recorders;
        private List<CommandRecorder> _usedRecorders;

        private bool _disposedValue;

        internal FrameGraphRecorder()
        {
            _recorders = new DisposableObjectPool<CommandRecorder>(new CommandRecorderPolicy());
            _usedRecorders = new List<CommandRecorder>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (CommandRecorder recorder in _usedRecorders)
                    {
                        recorder.Dispose();
                    }

                    _recorders.Dispose();
                    _usedRecorders.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearForFrame()
        {
            foreach (CommandRecorder recorder in _usedRecorders)
            {
                _recorders.Return(recorder);
            }

            _usedRecorders.Clear();
        }

        internal CommandRecorder GetNewRecorder()
        {
            CommandRecorder recorder = _recorders.Get();
            _usedRecorders.Add(recorder);

            return recorder;
        }

        private readonly record struct CommandRecorderPolicy : IObjectPoolPolicy<CommandRecorder>
        {
            public CommandRecorder Create() => new CommandRecorder();

            public bool Return(ref CommandRecorder obj)
            {
                obj.ResetForNewRecording();
                return true;
            }
        }
    }
}
