using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace VoxelizationDemo.Editor.Serialization
{
    internal ref struct SerializedPropertyInfo
    {
        public readonly ReadOnlySpan<byte> Name;
        public Utf8JsonReader Value;

        public SerializedPropertyInfo(ReadOnlySpan<byte> name, ref Utf8JsonReader value)
        {
            Name = name;
            Value = value;
        }
    }
}
