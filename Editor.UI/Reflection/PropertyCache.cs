using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Styling;
using Primary.Common;
using SharpGen.Runtime.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Reflection
{
    public sealed class PropertyCache
    {
        private Dictionary<PropertyKey, int> _properties;
        private Dictionary<PropertyKey, object?> _defaults;
        private Dictionary<Type, StyleCache> _caches;

        private Lock _cacheLock;
        private Lock _defaultLock;

        internal PropertyCache()
        {
            _properties = new Dictionary<PropertyKey, int>();
            _defaults = new Dictionary<PropertyKey, object?>();
            _caches = new Dictionary<Type, StyleCache>();

            _cacheLock = new Lock();
            _defaultLock = new Lock();
        }

        private void CacheNewInheritedBase(Type type)
        {
            using (_cacheLock.EnterScope())
            {
                UIManager.Logger?.Debug("Caching new type properties: {t}", type);

                PropertyInfo[] properties = type.GetProperties();
                FieldInfo[] fields = [];

                Type? templatedType = null;
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    templatedType = type;
                    type = type.GetGenericTypeDefinition();
                }

                {
                    IEnumerable<FieldInfo> temp = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    Type? baseType = type.BaseType;
                    while (baseType != null && baseType != typeof(object))
                    {
                        temp = temp.Concat(baseType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
                        baseType = baseType.BaseType;
                    }

                    fields = [.. temp];
                }

                (StyleProperty Property, Type Type)[] styleProperties = [.. properties
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
                            bool isFieldLess = editable.FieldName.Length == 0;

                            FieldInfo? field = null;
                            if (!isFieldLess)
                            {
                                field = Array.Find(fields, (x) => x.Name == editable.FieldName);
                                if (field == null)
                                {
                                    UIManager.Logger?.Warning("[{t}]: No corresponding field with name: {n} was found in the class", x.DeclaringType, editable.FieldName);
                                    continue;
                                }

                                Guard.Equals(x.PropertyType, field.FieldType);
                            }

                            StylePropertyFlags flags = StylePropertyFlags.None;

                            Type objectType = isFieldLess ? x.PropertyType : field!.FieldType;
                            Type ownerType = type;

                            if (objectType.IsGenericType)
                            {
                                // can't cache it because of the generic nature of the current field
                                if (templatedType == null)
                                    continue;

                                if (objectType.IsGenericTypeDefinition)
                                {
                                    UIManager.Logger?.Warning("[{t}]: Field or property {n} cannot be cached because it is not a generic type definition ", x.DeclaringType, editable.FieldName);
                                    continue;
                                }

                                ownerType = templatedType;
                                flags |= StylePropertyFlags.IsGenericType;
                            }
                            else if (objectType == typeof(object))
                            {
                                objectType = x.PropertyType;
                            }

                            return (new StyleProperty(
                                StylePropertyType.Editable,
                                editable.CustomName ?? x.Name,
                                field?.Name ?? editable.CustomName ?? x.Name,
                                objectType,
                                x,
                                field,
                                editable.Effects,
                                flags,
                                null), ownerType);
                        }
                        else if (attribute is StyleablePropertyAttribute styleable)
                        {
                            bool isFieldLess = styleable.FieldName.Length == 0;

                            FieldInfo? field = null;
                            if (!isFieldLess)
                            {
                                field = Array.Find(fields, (x) => x.Name == styleable.FieldName);
                                if (field == null)
                                {
                                    UIManager.Logger?.Warning("[{t}]: No corresponding field with name: {n} was found in the class", x.DeclaringType, styleable.FieldName);
                                    continue;
                                }

                                Guard.Equals(x.PropertyType, field.FieldType);
                            }

                            StylePropertyFlags flags = StylePropertyFlags.None;

                            Type objectType = isFieldLess ? x.PropertyType : field!.FieldType;
                            Type ownerType = type;

                            if (objectType.IsGenericType)
                            {
                                // can't cache it because of the generic nature of the current field
                                if (templatedType == null)
                                    continue;

                                if (objectType.IsGenericTypeDefinition)
                                {
                                    UIManager.Logger?.Warning("[{t}]: Field or property {n} cannot be cached because it is not a generic type definition ", x.DeclaringType, styleable.FieldName);
                                    continue;
                                }

                                ownerType = templatedType;
                                flags |= StylePropertyFlags.IsGenericType;
                            }
                            else if (objectType == typeof(object))
                            {
                                objectType = x.PropertyType;
                            }

                            object? defaultValue = null;

                            PropertyDefaultAttribute? defaultAttribute = isFieldLess ? x.GetCustomAttribute<PropertyDefaultAttribute>() : field!.GetCustomAttribute<PropertyDefaultAttribute>();
                            if (defaultAttribute != null)
                            {
                                if (UIManager.Instance.SerializationManager.ValueSerializerTable.Deserialize(objectType, defaultAttribute.DefaultValue, out defaultValue))
                                {
                                    using (_defaultLock.EnterScope())
                                    {
                                        _defaults.TryAdd(new PropertyKey(templatedType ?? type, styleable.CustomName ?? x.Name), defaultValue);
                                    }

                                    flags |= StylePropertyFlags.HasDefaultValue;
                                }
                                else
                                {
                                    UIManager.Logger?.Warning("[{t}]: Failed to parse default value for field: {f}", x.DeclaringType, styleable.FieldName);
                                }
                            }

                            return (new StyleProperty(StylePropertyType.Styleable, styleable.CustomName ?? x.Name, field?.Name ?? styleable.CustomName ?? x.Name, objectType, x, field, styleable.Effects, flags, defaultValue), ownerType);
                        }
                    }

                    throw new Exception();
                })];

                for (int i = 0; i < styleProperties.Length; ++i)
                {
                    var (property, owner) = styleProperties[i];

                    if (templatedType != null)
                        _properties[new PropertyKey(templatedType, property.Name)] = i;
                    _properties[new PropertyKey(type, property.Name)] = i;
                }

                _caches.Add(templatedType ?? type, new StyleCache([.. styleProperties.Select(static (x) => x.Property)]));

                try
                {
                    object? obj = Activator.CreateInstance(type);
                    if (obj != null)
                    {
                        InitializeDefaults(obj);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        internal void InitializeDefaults(object obj)
        {
            Type type = obj.GetType();
            Guard.IsFalse(type.IsGenericType && type.IsGenericTypeDefinition);
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier)));

            using (_cacheLock.EnterScope())
            {
                if (_caches.TryGetValue(type, out StyleCache? cache))
                {
                    if (cache.IsInitialized)
                        return;

                    StyleProperty[] properties = [.. cache.Properties];
                    for (int i = 0; i < properties.Length; i++)
                    {
                        ref StyleProperty prop = ref properties[i];
                        if (!prop.HasDefaultValue)
                        {
                            Type ownerType = !type.IsGenericTypeDefinition ?
                                (prop.IsGenericType ? type.GetGenericTypeDefinition() : type) :
                                type;
                            PropertyKey key = new PropertyKey(ownerType, prop.Name);

                            if (_defaults.TryGetValue(key, out object? defaultValue))
                                prop = new StyleProperty(prop.Type, prop.Name, prop.LocalName, prop.ObjectType, prop.Property, prop.Field, prop.Effects, prop.PropertyFlags | StylePropertyFlags.HasDefaultValue, defaultValue);
                            else if (prop.Field != null)
                            {
                                defaultValue = prop.Field.GetValue(obj);

                                _defaults.Add(key, defaultValue);
                                prop = new StyleProperty(prop.Type, prop.Name, prop.LocalName, prop.ObjectType, prop.Property, prop.Field, prop.Effects, prop.PropertyFlags | StylePropertyFlags.HasDefaultValue, defaultValue);
                            }
                        }
                    }

                    cache.Properties = [.. properties];
                    cache.IsInitialized = true;
                }
            }
        }

        public StyleCache GetCache(Type type)
        {
            Guard.IsFalse(type.IsGenericType && type.IsGenericTypeDefinition);
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier)));

            using (_cacheLock.EnterScope())
            {
                if (!_caches.TryGetValue(type, out StyleCache? value))
                {
                    CacheNewInheritedBase(type);
                    return _caches[type];
                }

                return value;
            }
        }

        public StyleProperty FindProperty(Type type, string propertyName)
        {
            Guard.IsFalse(type.IsGenericType && type.IsGenericTypeDefinition);
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier)));
            Guard.IsNotNullOrEmpty(propertyName);

            using (_cacheLock.EnterScope())
            {
                StyleCache cache = GetCache(type);
                PropertyKey key = new PropertyKey(type, propertyName);

                return _properties.TryGetValue(key, out int value) ? cache.Properties[value] : default;
            }
        }

        public bool TryFindProperty(Type type, string propertyName, out StyleProperty value)
        {
            Guard.IsFalse(type.IsGenericType && type.IsGenericTypeDefinition);
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier)));
            Guard.IsNotNullOrEmpty(propertyName);

            using (_cacheLock.EnterScope())
            {
                StyleCache cache = GetCache(type);
                PropertyKey key = new PropertyKey(type, propertyName);

                if (_properties.TryGetValue(key, out int index))
                {
                    value = cache.Properties[index];
                    return true;
                }
            }

            value = default;
            return false;
        }

        private readonly record struct PropertyKey(Type Type, string Name)
        {
            public override int GetHashCode() => Type.GetHashCode() ^ Name.GetDjb2HashCode();
        }

        private static readonly StyleCache s_emptyStyleCache = new StyleCache([]);
    }

    public sealed class StyleCache
    {
        private ImmutableArray<StyleProperty> _properties;
        private bool _isInitialized;

        internal StyleCache(ImmutableArray<StyleProperty> properties)
        {
            _properties = properties;
            _isInitialized = false;
        }

        public StyleProperty FindProperty(string propertyName)
        {
            foreach (StyleProperty prop in _properties)
            {
                if (prop.Name == propertyName)
                    return prop;
            }

            return default;
        }

        public int FindIndex(string propertyName)
        {
            for (int i = 0; i < _properties.Length; i++)
            {
                if (_properties[i].Name == propertyName)
                    return i;
            }

            return -1;
        }

        public ImmutableArray<StyleProperty> Properties { get => _properties; internal set => _properties = value; }
        public bool IsInitialized { get => _isInitialized; internal set => _isInitialized = value; }
    }

    public readonly record struct StyleProperty(StylePropertyType Type, string Name, string? LocalName, Type ObjectType, PropertyInfo Property, FieldInfo? Field, UIStateFlags Effects, StylePropertyFlags PropertyFlags, object? DefaultValue)
    {
        public bool HasDefaultValue => Flags.HasFlag(PropertyFlags, StylePropertyFlags.HasDefaultValue);
        public bool IsGenericType => Flags.HasFlag(PropertyFlags, StylePropertyFlags.IsGenericType);
    }

    public enum StylePropertyType : byte
    {
        Editable = 0,
        Styleable
    }

    [Flags]
    public enum StylePropertyFlags : byte
    {
        None = 0,

        HasDefaultValue = 1 << 0,
        IsGenericType = 1 << 1
    }
}
