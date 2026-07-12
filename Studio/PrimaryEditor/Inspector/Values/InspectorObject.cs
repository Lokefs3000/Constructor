using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Inspector.Reflection;

namespace PrimaryEditor.Inspector.Values
{
    public class InspectorObject<TObject, T> : InspectorValue<TObject, T>, IInspectorObject
    {
        internal InspectorObject(IInspectorObject? parentObject, InspectorValueSource valueSource) : base(parentObject, valueSource)
        {
        }
    }
}
