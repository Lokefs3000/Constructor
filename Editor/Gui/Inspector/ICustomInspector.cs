using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Inspector
{
    public interface ICustomInspector<T> : ICustomInspector
    {
        
    }

    public interface ICustomInspector
    {
        public void SetupInspectorData(InspectorContext context, Type type);
    }
}
