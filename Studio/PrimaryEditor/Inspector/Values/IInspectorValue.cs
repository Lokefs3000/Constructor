using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Inspector.Values
{
    public interface IInspectorValue : IEquatable<IInspectorValue>
    {
        public void UpdateValueFromValueType(ref OpaqueRef valueType);
        public void UpdateValueFromObject(object obj);

        public ref OpaqueRef GetValueType();
        public object? GetObjectValue();

        public Type TargetType { get; }
        public IInspectorValue? OwningValue { get; }

        public string TargetName { get; }
        public string FullTargetName { get; }

        public int UniqueHash { get; }
    }
}
