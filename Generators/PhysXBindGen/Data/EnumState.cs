using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.HighPerformance;
using PhysXBindGen.Exceptions;

namespace PhysXBindGen.Data
{
    internal sealed class EnumState : DatatypeState
    {
        private readonly string _name;
        private TypedefDatatype _datatype;

        private readonly List<EnumValue> _values;

        internal EnumState(string name, TypedefDatatype datatype)
        {
            _name = name;
            _datatype = datatype;

            _values = new List<EnumValue>();
        }

        public void AddValue(string name, long value)
        {
            foreach (EnumValue enumValue in _values)
            {
                if (enumValue.Name == name)
                {
                    throw new DuplicateStateException($"Enum value with name '{name}' already exists");
                }
            }

            _values.Add(new EnumValue(name, value));
        }

        public void OverflowValues(TypedefDatatype datatype)
        {
            if (_datatype != datatype)
            {
                _datatype = datatype;

                foreach (ref EnumValue enumValue in _values.AsSpan())
                {
                    enumValue = new EnumValue(enumValue.Name, datatype switch
                    {
                        TypedefDatatype.Int8 => (sbyte)enumValue.Value,
                        TypedefDatatype.UInt8 => (byte)enumValue.Value,
                        TypedefDatatype.Int16 => (short)enumValue.Value,
                        TypedefDatatype.UInt16 => (ushort)enumValue.Value,
                        TypedefDatatype.Int32 => (int)enumValue.Value,
                        TypedefDatatype.UInt32 => (uint)enumValue.Value,
                        TypedefDatatype.Int64 => enumValue.Value,
                        TypedefDatatype.UInt64 => enumValue.Value,
                        _ => throw new NotImplementedException(),
                    });
                }
            }
        }

        public bool TryGetValue(ReadOnlySpan<char> enumName, [NotNullWhen(true)] out long value)
        {
            foreach (ref EnumValue enumValue in _values.AsSpan())
            {
                if (enumName.Equals(enumValue.Name, StringComparison.Ordinal))
                {
                    value = enumValue.Value;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public bool TryGetName(long value, [NotNullWhen(true)] out string? name)
        {
            foreach (ref EnumValue enumValue in _values.AsSpan())
            {
                if (enumValue.Value == value)
                {
                    name = enumValue.Name;
                    return true;
                }
            }

            name = null;
            return false;
        }

        public string Name => _name;
        public TypedefDatatype Datatype => _datatype;

        public List<EnumValue> Values => _values;
    }

    internal readonly record struct EnumValue(string Name, long Value);
}
