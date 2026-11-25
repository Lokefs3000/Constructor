using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Inspector
{
    internal class InspectorValue<T> : InspectorObject<T>
    {
        public InspectorValue(InspectorManager manager, InspectorObject<T>? parent, string propertyName, InspectorField field) : base(manager, parent, propertyName, field)
        {

        }
    }
}
