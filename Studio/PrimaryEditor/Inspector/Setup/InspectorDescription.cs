using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

        public override string ToString() => $"{_type.FullName}({_values.Length})";

        public Type Type => _type;
        public ImmutableArray<DescriptionValue> Values => _values;
    }
}
