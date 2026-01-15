using Primary.Pooling;

namespace Primary.Rendering.Recording
{
    public sealed class FrameGraphRecorder : IDisposable
    {
        private DisposableObjectPool<CommandRecorder> _recorders;
        private Dictionary<int, CommandRecorder> _usedRecorders;

        private bool _disposedValue;

        internal FrameGraphRecorder(RenderPassManager manager)
        {
            _recorders = new DisposableObjectPool<CommandRecorder>(new CommandRecorderPolicy(manager));
            _usedRecorders = new Dictionary<int, CommandRecorder>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var recorder in _usedRecorders)
                    {
                        recorder.Value.Dispose();
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
            foreach (var recorder in _usedRecorders)
            {
                _recorders.Return(recorder.Value);
            }

            _usedRecorders.Clear();
        }

        internal CommandRecorder GetNewRecorder(int passIndex)
        {
            CommandRecorder recorder = _recorders.Get();
            _usedRecorders.Add(passIndex, recorder);

            return recorder;
        }

        internal CommandRecorder? GetRecorderForPass(int passIndex)
        {
            if (_usedRecorders.TryGetValue(passIndex, out CommandRecorder? recorder))
                return recorder;

            return null;
        }

        private readonly record struct CommandRecorderPolicy(RenderPassManager Manager) : IObjectPoolPolicy<CommandRecorder>
        {
            public CommandRecorder Create() => new CommandRecorder(Manager);

            public bool Return(ref CommandRecorder obj)
            {
                obj.ResetForNewRecording();
                return true;
            }
        }
    }
}
