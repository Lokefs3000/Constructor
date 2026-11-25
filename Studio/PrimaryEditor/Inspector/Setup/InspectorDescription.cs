using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace PrimaryEditor.Inspector.Setup
{
    public sealed class InspectorDescription
    {
        private readonly Type _type;
        private readonly ImmutableArray<DescriptionValue> _values;

        internal InspectorDescription(Type type, ImmutableArray<DescriptionValue> values)
        {
            _type = type;
            _values = values;
        }

        internal bool HasReferencesTo(Assembly assembly)
        {
            foreach (DescriptionValue value in _values)
            {
                if (value.Description?.HasReferencesTo(assembly) ?? (value.ValueType.Assembly == assembly))
                    return true;
            }

            return _type.Assembly == assembly;
        }

        public override string ToString() => $"{_type.FullName}({_values.Length})";

        public Type Type => _type;
        public ImmutableArray<DescriptionValue> Values => _values;
    }
}
