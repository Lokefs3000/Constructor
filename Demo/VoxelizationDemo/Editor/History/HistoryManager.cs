using Primary.Input;
using Primary.Input.Devices;
using Primary.Threading;

namespace VoxelizationDemo.Editor.History
{
    public sealed class HistoryManager : IDisposable
    {
        private readonly List<HistoryGroup> _groups;
        private int _groupIndex;

        private HistoryGroup? _currentGroup;

        private bool _disposedValue;

        internal HistoryManager()
        {
            _groups = new List<HistoryGroup>();
            _groupIndex = 0;

            _currentGroup = null;

            InputSystem.Pointer.OnMouseReleased += (_) => ThreadHelper.ExecuteOnMainThread(Flush);
            InputSystem.Keyboard.KeyReleased += (key) => { if (key == KeyCode.Return) ThreadHelper.ExecuteOnMainThread(Flush); };
        }

        public void Record(IHistoryStep step)
        {
            _currentGroup ??= new HistoryGroup();
            _currentGroup.AddStep(step);
        }

        public void Flush()
        {
            if (_currentGroup != null && !_currentGroup.IsEmpty)
            {
                if (_groupIndex < _groups.Count)
                {
                    for (int i = _groupIndex; i < _groups.Count; ++i)
                    {
                        _groups[i].ReturnToPool();
                    }

                    _groups.RemoveRange(_groupIndex, _groups.Count - _groupIndex);
                }

                _groups.Add(_currentGroup);
                _groupIndex = _groups.Count;
            }
            else
            {
                _currentGroup?.ReturnToPool();
            }

            _currentGroup = null;
        }

        public void Undo()
        {
            if (_groupIndex > 0)
            {
                _groups[--_groupIndex].Undo();
            }
        }

        public void Redo()
        {
            if (_groupIndex < _groups.Count)
            {
                _groups[_groupIndex++].Redo();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    for (int i = _groups.Count - 1; i >= 0; i--)
                    {
                        HistoryGroup group = _groups[i];
                        group.ReturnToPool();
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
    }
}
