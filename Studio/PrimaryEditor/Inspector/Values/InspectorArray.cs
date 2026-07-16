using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Inspector.Reflection;

namespace PrimaryEditor.Inspector.Values
{
    public class InspectorArray<TObject, T> : InspectorValue<TObject, T[]>, IInspectorObject, IInspectorArray
    {
        internal InspectorArray(IInspectorObject? parentObject, InspectorValueSource valueSource, string name) : base(parentObject, valueSource, name)
        {
        }
    }
}
