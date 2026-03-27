using CommunityToolkit.Diagnostics;
using Editor.UI.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CustomSerilizationRoutineAttribute : Attribute
    {
        private Type _routineType;

        public CustomSerilizationRoutineAttribute(Type routineType)
        {
            Guard.IsTrue(routineType.IsAssignableTo(typeof(ISerializationRoutine)));

            _routineType = routineType;
        }

        public Type RoutineType => _routineType;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class UIElementPrettyName(string PrettyName) : Attribute
    {
        private readonly string _prettyName = PrettyName;

        public string PrettyName => _prettyName;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModifierPrettyName(string PrettyName) : Attribute
    {
        private readonly string _prettyName = PrettyName;

        public string PrettyName => _prettyName;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class UIElementStates(params string[] States) : Attribute
    {
        private readonly string[] _states = States;

        public string[] States => _states;
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class EditablePropertyAttribute(string FieldName, UIStateFlags Effects = UIStateFlags.None, string? CustomName = null) : Attribute
    {
        private readonly string _fieldName = FieldName;
        private readonly UIStateFlags _effects = Effects;
        private readonly string? _customName = CustomName;

        public string FieldName => _fieldName;
        public UIStateFlags Effects => _effects;
        public string? CustomName => _customName;
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class StyleablePropertyAttribute(string FieldName, UIStateFlags Effects = UIStateFlags.None, string? CustomName = null) : Attribute
    {
        private readonly string _fieldName = FieldName;
        private readonly UIStateFlags _effects = Effects;
        private readonly string? _customName = CustomName;

        public string FieldName => _fieldName;
        public UIStateFlags Effects => _effects;
        public string? CustomName => _customName;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ValueSerializerTargetAttribute(Type TargetType) : Attribute
    {
        private readonly Type _targetType = TargetType;

        public Type TargetType => _targetType;
    }
}
