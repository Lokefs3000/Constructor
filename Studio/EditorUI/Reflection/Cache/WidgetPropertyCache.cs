using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using EditorUI.Serialization.Value;
using EditorUI.Styling;
using EditorUI.Widgets;
using Primary.Collections;

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
                UILog.Logger?.Debug("No public parameterless constructor on the type '{t}'", type);
                //UILog.Logger?.Error("Failed to find public parameterless constructor on widget of type '{t}'", type);
                //throw new NotSupportedException();
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Dictionary<string, FieldInfo> fieldDict = fields.ToDictionary(static (x) => x.Name);

            Dictionary<string, MethodInfo> callbackDelegates = new Dictionary<string, MethodInfo>();
            Dictionary<string, (string[] Callbacks, int Length)> callbackFields = new Dictionary<string, (string[] Callbacks, int Length)>();

            for (int i = 0; i < methods.Length; ++i)
            {
                MethodInfo method = methods[i];
                StyleUpdateCallbackAttribute? attribute = method.GetCustomAttribute<StyleUpdateCallbackAttribute>();

                if (attribute != null)
                {
                    string callbackName = $"{type.Name}.{method.Name}";

                    if (method.GetParameters().Length > 0)
                    {
                        UILog.Logger?.Warning("The style callback '{c}' will not be called because it contains parameters", callbackName);
                        continue;
                    }

                    if (attribute.PropertyNames.IsEmpty)
                    {
                        UILog.Logger?.Warning("No properties specified for style callback '{c}'. This is redundant and more than likely unintended", callbackName);
                        continue;
                    }

                    if (!callbackDelegates.TryAdd(callbackName, method))
                    {
                        UILog.Logger?.Warning("Duplicate style callback with the name '{c}'. Uhhh, I didn't think this could actually happen", callbackName);
                        continue;
                    }

                    for (int j = 0; j < attribute.PropertyNames.Length; ++j)
                    {
                        ref var callbackData = ref CollectionsMarshal.GetValueRefOrAddDefault(callbackFields, attribute.PropertyNames[j], out bool exist);
                        if (exist)
                        {
                            if (callbackData.Callbacks.Length == callbackData.Length)
                                ArrayPool<string>.Shared.Resize(ref callbackData.Callbacks, callbackData.Callbacks.Length * 2, true);
                            callbackData.Callbacks[callbackData.Length++] = callbackName;
                        }
                        else
                        {
                            string[] callbacks = ArrayPool<string>.Shared.Rent(8);
                            callbackData = (callbacks, 1);

                            callbacks[0] = callbackName;
                        }
                    }
                }
            }

            // find all trigger properties to generate a proper trigger mask
            //  NOTE: likely redundant atleast in this state of the trigger system
            Dictionary<PropertyInfo, ushort> triggerProperties = new Dictionary<PropertyInfo, ushort>();
            for (int i = 0; i < properties.Length; ++i)
            {
                PropertyInfo propertyInfo = properties[i];
                StyleTriggerAttribute? attribute = propertyInfo.GetCustomAttribute<StyleTriggerAttribute>();

                if (attribute != null)
                {
                    triggerProperties.Add(propertyInfo, /*(ushort)(1 << attribute.Priority)*/TriggerValues.Triggers[JsonNamingPolicy.KebabCaseLower.ConvertName(propertyInfo.Name)]);
                }
            }

            // generate a 'PropertyData' object for each serialized property
            Dictionary<string, PropertyData> propertyData = new Dictionary<string, PropertyData>();
            for (int i = 0; i < properties.Length; ++i)
            {
                PropertyInfo propertyInfo = properties[i];
                StyledAttribute? attribute = propertyInfo.GetCustomAttribute<StyledAttribute>();

                if (attribute != null)
                {
                    if (propertyInfo.GetMethod == null)
                    {
                        UILog.Logger?.Warning("Missing Get method on serialized property '{n}' on the widget of type '{t}'", propertyInfo.Name, type);
                        continue;
                    }

                    FieldInfo? fieldInfo = null;
                    if (attribute.FieldName != null)
                    {
                        if (!fieldDict.TryGetValue(attribute.FieldName, out fieldInfo))
                        {
                            UILog.Logger?.Warning("Failed to find the field with specified name on property '{n}' on the widget of type '{t}'", propertyInfo.Name, type);
                            continue;
                        }
                    }

                    PropertyDataFlags dataFlags = PropertyDataFlags.None;

                    if (attribute.IsEditable)
                        dataFlags |= PropertyDataFlags.IsEditable;
                    if (attribute.EffectsParent)
                        dataFlags |= PropertyDataFlags.EffectsParent;

                    ushort triggerMask = 0;
                    bool hasTriggerMask = triggerProperties.TryGetValue(propertyInfo, out triggerMask);

                    if (!callbackFields.TryGetValue(propertyInfo.Name, out var tuple))
                        tuple = ([], 0);

                    StyleConverterTypesAttribute? converterTypesAttribute = propertyInfo.GetCustomAttribute<StyleConverterTypesAttribute>();

                    StyleProperty styleProperty = new StyleProperty(propertyInfo);

                    propertyData.Add(propertyInfo.Name, new PropertyData(
                        propertyInfo.Name,
                        JsonNamingPolicy.KebabCaseLower.ConvertName(propertyInfo.Name),
                        dataFlags,
                        attribute.StateFlags,
                        converterTypesAttribute?.Types ?? [propertyInfo.PropertyType],
                        hasTriggerMask ? triggerMask : null,
                        styleProperty,
                        fieldInfo,
                        _manager.MethodGenerator.EmitMethods(propertyInfo, fieldInfo),
                        tuple.Length == 0 ? [] : tuple.Callbacks[..tuple.Length]));
                }
            }

            if (callbackFields.Count > 0)
            {
                foreach (var (key, (items, length)) in callbackFields)
                {
                    Array.Clear(items, 0, length);
                    ArrayPool<string>.Shared.Return(items);
                }
            }

            return new WidgetCachedData(type, propertyData.ToFrozenDictionary(), callbackDelegates.ToFrozenDictionary(), constructorInfo);
        }

        public WidgetCachedData GetCachedData(Type type)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyledObject)));

            return _cachedData.GetOrAdd(type, CacheNewWidgetData);
        }

        public static WidgetPropertyCache Instance => UIManager.Instance.ReflectionManager.WidgetPropertyCache;
    }
}
