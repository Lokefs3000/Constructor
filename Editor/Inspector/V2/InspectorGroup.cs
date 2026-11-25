using System;
using System.Collections.Generic;
using System.Text;
using Editor.Inspector.V2.Values;

namespace Editor.Inspector.V2
{
    public abstract class InspectorGroup
    {
        protected List<IInspectorValue> _values;

        internal InspectorGroup()
        {
            _values = new List<IInspectorValue>();
        }
    }
}
