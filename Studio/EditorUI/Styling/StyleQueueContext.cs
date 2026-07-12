using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;

namespace EditorUI.Styling
{
    public record struct StyleQueueContext
    {
        private readonly Queue<StyleManager.StyleObjectSearch> _queue;
        private readonly bool _forceRestyle;

        private int _childIndex;

        internal StyleQueueContext(Queue<StyleManager.StyleObjectSearch> queue, bool forceRestyle)
        {
            _queue = queue;
            _forceRestyle = forceRestyle;
            _childIndex = 0;
        }

        public void TryEnqueue(StyledObject obj)
        {
            if (_forceRestyle || obj.StateFlags.HasFlags(StateFlags.InvalidStyle))
                _queue.Enqueue(new StyleManager.StyleObjectSearch(obj, _childIndex++));
        }
    }
}
