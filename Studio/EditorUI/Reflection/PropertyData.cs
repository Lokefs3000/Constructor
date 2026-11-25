using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EditorUI.Reflection.Dynamic;

namespace EditorUI.Reflection
{
    public sealed record class PropertyData(
        string Name,
        string StyleFriendlyName,
        PropertyDataFlags Flags,
        ulong PropertyMask,
        StateFlags StateFlags,
        Type[] PropertyTypes,
        object PropertyOrField,
        PropertyMethods Methods,
        string[]? Callbacks,
        PropertyLink[]? Links);

    public readonly record struct PropertyLink(string Name, PropertyMethods Methods, object? Value);

    [Flags]
    public enum PropertyDataFlags : byte
    {
        None = 0,

        IsEditable = 1 << 0,

        EffectsParent = 1 << 1
    }
}
