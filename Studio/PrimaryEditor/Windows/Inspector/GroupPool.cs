using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Inspector;

namespace PrimaryEditor.Windows.Inspector
{
    internal sealed class GroupPool : IDisposable
    {
        private readonly InspectorWindow _window;

        private readonly Stack<WidgetGroup> _groups;

        private bool _disposedValue;

        internal GroupPool(InspectorWindow window)
        {
            _window = window;

            _groups = new Stack<WidgetGroup>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    while (_groups.TryPop(out WidgetGroup? group))
                    {
                        group.Dispose();
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

        internal WidgetGroup GetWidgetGroup(InspectorGroup group)
        {
            if (!_groups.TryPop(out WidgetGroup? result))
                result = new WidgetGroup(_window);

            result.SetupGroupForHash(group.UniqueHash);
            result.AddGroup(group);

            return result;
        }
    }
}
