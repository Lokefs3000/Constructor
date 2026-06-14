using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Widgets
{
    public sealed class WidgetManager
    {
        private List<Widget> _queuedDestroys;

        internal WidgetManager()
        {
            _queuedDestroys = new List<Widget>();
        }

        internal void DestroyAllInQueue()
        {
            for (int i = 0; i < _queuedDestroys.Count; i++)
            {
                Widget widget = _queuedDestroys[i];
                if (!widget.IsDestroyed)
                {
                    // in-case of the child being in an auto layout within the parent
                    widget.Parent?.AddStateFlags(StateFlags.SelfInvalidLayout);
                    widget.DestroySelf();
                }
            }

            _queuedDestroys.Clear();
        }

        internal void QueueDestroy(Widget widget)
        {
            if (!_queuedDestroys.Contains(widget))
            {
                _queuedDestroys.Add(widget);
            }
        }

        public static WidgetManager Instance => UIManager.Instance.WidgetManager;
    }
}
