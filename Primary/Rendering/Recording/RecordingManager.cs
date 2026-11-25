using System;
using System.Collections.Generic;
using System.Text;
using Primary.Pooling;

namespace Primary.Rendering.Recording
{
    public sealed class RecordingManager : IDisposable
    {
        private readonly List<CommandRecorder> _storedRecorders;
        private int _activeRecorders;

        private bool _disposedValue;

        internal RecordingManager()
        {
            _storedRecorders = new List<CommandRecorder>();
            _activeRecorders = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (CommandRecorder recorder in _storedRecorders)
                    {
                        recorder.Dispose();
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

        internal void ReclaimRecorders()
        {
            for (int i = 0; i < _activeRecorders; ++i)
            {
                _storedRecorders[i].ClearRecording();
            }
        }

        internal CommandRecorder GetRecorder()
        {
            if (_activeRecorders == _storedRecorders.Count)
            {
                CommandRecorder recorder = new CommandRecorder();
                _storedRecorders.Add(recorder);
            }

            return _storedRecorders[_activeRecorders++];
        }
    }
}
