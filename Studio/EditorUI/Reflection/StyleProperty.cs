using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EditorUI.Reflection
{
    public readonly record struct StyleProperty
    {
        private readonly PropertyInfo _propertyInfo;

        public StyleProperty(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
        }

        public readonly void SetValue(object? obj, object? value) => _propertyInfo.SetValue(obj, value);

        public override string ToString() => _propertyInfo.Name;

        public readonly override int GetHashCode() => _propertyInfo.GetHashCode();
    }
}
