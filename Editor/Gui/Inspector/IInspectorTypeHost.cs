using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Inspector
{
    public interface IInspectorTypeHost<T>
    {
        public void SetupFor(ref T value, object? arg);
        public void Cleanup();
    }
}
