using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace EditorUI.Reflection.Cache
{
    public sealed class WidgetCachedData
    {
        private readonly Type _widgetType;

        private readonly FrozenDictionary<string, PropertyData> _properties;
        private readonly ImmutableArray<PropertyData> _propertyArray;

        private readonly ConstructorInfo? _constructor;

        internal WidgetCachedData(Type widgetType, FrozenDictionary<string, PropertyData> properties, ConstructorInfo? constructor)
        {
            _widgetType = widgetType;

            _properties = properties;
            _propertyArray = [.. properties.Values];

            _constructor = constructor;
        }

        public bool TryGetPropertyData(string name, [NotNullWhen(true)] out PropertyData? propertyData)
        {
            return _properties.TryGetValue(name, out propertyData);
        }

        public ImmutableArray<PropertyData> Properties => _propertyArray;
    }
}
