using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class StyledAttribute(string? fieldName, StateFlags stateFlags = StateFlags.None, bool isEditable = false, bool effectsParent = false) : Attribute
    {
        private readonly string? _fieldName = fieldName;
        private readonly StateFlags _stateFlags = stateFlags;
        private readonly bool _isEditable = isEditable;
        private readonly bool _effectsParent = effectsParent;

        public string? FieldName => _fieldName;
        public StateFlags StateFlags => _stateFlags;
        public bool IsEditable => _isEditable;
        public bool EffectsParent => _effectsParent;
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class StyleTriggerAttribute(int priority = 0) : Attribute
    {
        private readonly int _priority = priority;

        public int Priority => _priority;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ValueConverterAttribute : Attribute
    {
    }
}
