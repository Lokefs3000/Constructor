using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Text;

namespace EditorUI
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class StyleIncludeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class StyleSetupAttribute(StateFlags stateFlags = StateFlags.None) : StyleIncludeAttribute
    {
        private readonly StateFlags _stateFlags = stateFlags;

        public StateFlags StateFlags => _stateFlags;
        public bool IsGroup { init; get; }
        public bool Flatten { init; get; }
        public string? AliasAs { init; get; }
        public Type[]? ConverterTypes { init; get; }
        public bool IsEditable { init; get; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class StyleLink(string Name, object? Value) : StyleIncludeAttribute
    {
        private readonly string _name = Name;
        private readonly object? _value = Value;

        public string Name => _name;
        public object? Value => _value;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class StyleCondition(string Name, object? Value) : StyleIncludeAttribute
    {
        private readonly string _name = Name;
        private readonly object? _value = Value;

        public string Name => _name;
        public object? Value => _value;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class StyleUpdateCallbackAttribute(params string[] Names) : StyleIncludeAttribute
    {
        private readonly string[] _names = Names;

        public string[] Names => _names;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class TriggerValuesAttribute(params string[] Names) : StyleIncludeAttribute
    {
        private readonly string[] _names = Names;

        public string[] Names => _names;
    }
}
