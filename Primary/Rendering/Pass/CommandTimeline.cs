using Primary.Rendering.Memory;
using Primary.Rendering.Recording;

namespace Primary.Rendering.Pass
{
    internal sealed class CommandTimeline : IDisposable
    {
        private SequentialLinearAllocator _allocator;
        private List<TimelineAction> _actions;

        private bool _disposedValue;

        internal CommandTimeline()
        {
            _allocator = new SequentialLinearAllocator(2048);
            _actions = new List<TimelineAction>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _allocator.Dispose();
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

    internal record struct TimelineAction(int InstructionCount, uint DataOffset);
    internal record struct TimelineActionMeta(RecCommandType CommandType, RecCommandEffectFlags Effect);
}
