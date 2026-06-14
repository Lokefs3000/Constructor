using System;
using System.Collections.Generic;
using System.Text;
using Editor.Inspector.Layout;

namespace Editor.Inspector
{
    public interface IInspectorBase
    {
        public void UpdateValueByRef(ref GenericRefValue baseTarget);
        public void UpdateValueObject(GenericRefObject baseTarget);

        public ref GenericRefValue GetRefValue();
        public GenericRefObject GetRefObject();

        public LayoutObject Object { get; }
        public IInspectorBase? Parent { get; }

        public string PropertyName { get; }
        public Type PropertyType { get; }

        public bool IsValueAClass { get; }
    }
}
