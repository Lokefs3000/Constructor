using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Inspector.V2.Values
{
    public interface IInspectorValue
    {
        public void UpdateValueFromValueType(ref OpaqueRef valueType);
        public void UpdateValueFromObject(object obj);

        public bool Equals(IInspectorValue other);
    }
}
