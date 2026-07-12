using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EditorUI.Reflection.Dynamic;

namespace EditorUI.Reflection
{
    public record class PropertyData(string Name, string StyleFriendlyName, PropertyDataFlags Flags, StateFlags StateFlags, Type[] PropertyTypes, ushort? TriggerMask, StyleProperty Property, FieldInfo? Field, PropertyMethods Methods, string[]? Callbacks);

    [Flags]
    public enum PropertyDataFlags : byte
    {
        None = 0,

        IsEditable = 1 << 0,

        EffectsParent = 1 << 1
    }
}
