using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EditorUI.Reflection.Dynamic;

namespace EditorUI.Reflection
{
    public record class PropertyData(string Name, PropertyDataFlags Flags, StateFlags StateFlags, Type PropertyType, ushort TriggerMask, StyleProperty Property, FieldInfo? Field, PropertyMethods Methods);

    [Flags]
    public enum PropertyDataFlags : byte
    {
        None = 0,

        IsEditable = 1 << 0,

        EffectsParent = 1 << 1
    }
}
