using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace VoxelizationDemo.Editor.Serialization
{
    internal readonly record struct SerializationBag(ImmutableArray<SerializationProperty> Properties)
    {
        public static readonly SerializationBag Empty = new SerializationBag([]);
    }
}
