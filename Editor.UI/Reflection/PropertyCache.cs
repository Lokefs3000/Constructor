using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Styling;
using SharpGen.Runtime.Win32;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Editor.UI.Reflection
{
    public sealed class PropertyCache
    {
        private Dictionary<PropertyKey, int> _properties;
        private Dictionary<Type, StyleCache> _caches;

        internal PropertyCache()
        {
            _properties = new Dictionary<PropertyKey, int>();
            _caches = new Dictionary<Type, StyleCache>();
        }

        private void CacheNewInheritedBase(Type type)
        {
            UIManager.Logger?.Debug("Caching new type properties: {t}", type);

            PropertyInfo[] properties = type.GetProperties();
            FieldInfo[] fields = Array.Empty<FieldInfo>();

            {
                IEnumerable<FieldInfo> temp = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                Type? baseType = type.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    temp = temp.Concat(baseType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
                    baseType = baseType.BaseType;
                }

                fields = temp.ToArray();
            }

            object? value = null;
            if (type.IsAssignableTo(typeof(UIElement)))
            {
                if (UIManager.Instance.ReflectionManager.ElementCache.TryGetElementData(type, out CachedElementData elementData))
                {
                    _caches.Add(type, s_emptyStyleCache);
                    value = elementData.Constructor.Invoke(null);
                    _caches.Remove(type);
                }
                else
                    UIManager.Logger?.Warning("Failed to create default ui element object");
            }

            StyleProperty[] styleProperties = properties
                .Where((x) =>
                {
                    foreach (Attribute attribute in x.GetCustomAttributes())
                    {
                        if (attribute is EditablePropertyAttribute or StyleablePropertyAttribute)
                        {
                            return true;
                        }
                    }

                    return false;
                })
                .Select((x) =>
                {
                    foreach (Attribute attribute in x.GetCustomAttributes())
                    {
                        if (attribute is EditablePropertyAttribute editable)
                        {
                            FieldInfo? field = Array.Find(fields, (x) => x.Name == editable.FieldName);
                            if (field == null)
                            {
                                UIManager.Logger?.Warning("[{t}]: No corresponding field with name: {n} was found in the class", x.DeclaringType, editable.FieldName);
                                continue;
                            }

                            Guard.Equals(x.PropertyType, field.FieldType);
                            return new StyleProperty(StylePropertyType.Editable, editable.CustomName ?? x.Name, field.FieldType, x, field, editable.Effects, false, null);
                        }
                        else if (attribute is StyleablePropertyAttribute styleable)
                        {
                            FieldInfo? field = Array.Find(fields, (x) => x.Name == styleable.FieldName);
                            if (field == null)
                            {
                                UIManager.Logger?.Warning("[{t}]: No corresponding field with name: {n} was found in the class", x.DeclaringType, styleable.FieldName);
                                continue;
                            }

                            Guard.Equals(x.PropertyType, field.FieldType);
                            return new StyleProperty(StylePropertyType.Styleable, styleable.CustomName ?? x.Name, field.FieldType, x, field, styleable.Effects, value != null, value == null ? null : field.GetValue(value));
                        }
                    }

                    throw new Exception();
                }).ToArray();

            for (int i = 0; i < styleProperties.Length; ++i)
            {
                _properties[new PropertyKey(type, styleProperties[i].Name)] = i;
            }

            _caches.Add(type, new StyleCache(styleProperties));
        }

        public StyleCache GetCache(Type type)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier)));

            if (!_caches.TryGetValue(type, out StyleCache value))
            {
                CacheNewInheritedBase(type);
                return _caches[type];
            }

            return value;
        }

        public StyleProperty FindProperty(Type type, string propertyName)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier)));
            Guard.IsNotNullOrEmpty(propertyName);

            StyleCache cache = GetCache(type);
            PropertyKey key = new PropertyKey(type, propertyName);

            return _properties.TryGetValue(key, out int value) ? cache.Properties[value] : default;
        }

        public bool TryFindProperty(Type type, string propertyName, out StyleProperty value)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier)));
            Guard.IsNotNullOrEmpty(propertyName);

            StyleCache cache = GetCache(type);
            PropertyKey key = new PropertyKey(type, propertyName);

            if (_properties.TryGetValue(key, out int index))
            {
                value = cache.Properties[index];
                return true;
            }

            value = default;
            return false;
        }

        private readonly record struct PropertyKey(Type Type, string Name)
        {
            public override int GetHashCode() => Type.GetHashCode() ^ Name.GetDjb2HashCode();
        }

        private static readonly StyleCache s_emptyStyleCache = new StyleCache(Array.Empty<StyleProperty>());
    }

    public readonly record struct StyleCache(StyleProperty[] Properties)
    {
        public StyleProperty FindProperty(string propertyName) => Array.Find(Properties, (x) => x.Property.Name == propertyName);
    }

    public readonly record struct StyleProperty(StylePropertyType Type, string Name, Type ObjectType, PropertyInfo Property, FieldInfo Field, UIStateFlags Effect, bool HasDefaultValue, object? DefaultValue)
    {

    }

    public enum StylePropertyType : byte
    {
        Editable = 0,
        Styleable
    }
}
