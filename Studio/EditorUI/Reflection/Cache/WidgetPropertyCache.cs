using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Widgets;

namespace EditorUI.Reflection.Cache
{
    public sealed class WidgetPropertyCache
    {
        private readonly ReflectionManager _manager;

        private ConcurrentDictionary<Type, WidgetCachedData> _cachedData;

        internal WidgetPropertyCache(ReflectionManager manager)
        {
            _manager = manager;

            _cachedData = new ConcurrentDictionary<Type, WidgetCachedData>();
        }

        private WidgetCachedData CacheNewWidgetData(Type type)
        {
            // find constructor for serialization
            ConstructorInfo? constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
            if (constructorInfo == null)
            {
                UILog.Logger?.Error("Failed to find public parameterless constructor on widget of type '{t}'", type);
                throw new NotSupportedException();
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            Dictionary<string, FieldInfo> fieldDict = fields.ToDictionary(static (x) => x.Name);

            // find all trigger properties to generate a proper trigger mask
            //  NOTE: likely redundant atleast in this state of the trigger system
            Dictionary<PropertyInfo, ushort> triggerProperties = new Dictionary<PropertyInfo, ushort>();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo propertyInfo = properties[i];
                StyleTriggerAttribute? attribute = propertyInfo.GetCustomAttribute<StyleTriggerAttribute>();

                if (attribute != null)
                {
                    triggerProperties.Add(propertyInfo, (ushort)(1 << attribute.Priority));
                }
            }

            // generate a 'PropertyData' object for each serialized property
            Dictionary<string, PropertyData> propertyData = new Dictionary<string, PropertyData>();
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo propertyInfo = properties[i];
                StyledAttribute? attribute = propertyInfo.GetCustomAttribute<StyledAttribute>();

                if (attribute != null)
                {
                    if (propertyInfo.GetMethod == null || propertyInfo.SetMethod == null)
                    {
                        UILog.Logger?.Warning("Missing Get and/or Set methods on serialized property '{n}' on the widget of type '{t}'", propertyInfo.Name, type);
                        continue;
                    }

                    FieldInfo? fieldInfo = null;
                    if (attribute.FieldName != null)
                    {
                        if (!fieldDict.TryGetValue(attribute.FieldName, out fieldInfo))
                        {
                            UILog.Logger?.Warning("Failed to find field with specified name on property '{n}' on the widget of type '{t}'", propertyInfo.Name, type);
                            continue;
                        }
                    }

                    PropertyDataFlags dataFlags = PropertyDataFlags.None;

                    if (attribute.IsEditable)
                        dataFlags |= PropertyDataFlags.IsEditable;
                    if (attribute.EffectsParent)
                        dataFlags |= PropertyDataFlags.EffectsParent;

                    ushort triggerMask = 0;
                    triggerProperties.TryGetValue(propertyInfo, out triggerMask);

                    StyleProperty styleProperty = new StyleProperty(propertyInfo);

                    propertyData.Add(propertyInfo.Name, new PropertyData(
                        propertyInfo.Name,
                        dataFlags,
                        attribute.StateFlags,
                        propertyInfo.PropertyType,
                        triggerMask,
                        styleProperty,
                        fieldInfo,
                        _manager.MethodGenerator.EmitMethods(propertyInfo, fieldInfo)));
                }
            }

            return new WidgetCachedData(type, propertyData.ToFrozenDictionary(), constructorInfo);
        }

        public WidgetCachedData GetCachedData(Type type)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(Widget)));

            return _cachedData.GetOrAdd(type, CacheNewWidgetData);
        }

        public static WidgetPropertyCache Instance => UIManager.Instance.ReflectionManager.WidgetPropertyCache;
    }
}
