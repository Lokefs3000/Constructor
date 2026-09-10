using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using EditorUI.Common;
using EditorUI.Reflection.Dynamic;
using EditorUI.Serialization.Value;
using EditorUI.Styling;
using Primary.Collections;
using Primary.Common;

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

            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Dictionary<string, MethodInfo> callbackDelegates = new Dictionary<string, MethodInfo>();
            Dictionary<string, (string[] Callbacks, int Length)> callbackTargets = new Dictionary<string, (string[] Callbacks, int Length)>();

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

                    if (attribute.Names.Length == 0)
                    {
                        UILog.Logger?.Warning("No properties specified for style callback '{c}'. This is redundant and more than likely unintended", callbackName);
                        continue;
                    }

                    if (!callbackDelegates.TryAdd(callbackName, method))
                    {
                        UILog.Logger?.Warning("Duplicate style callback with the name '{c}'. Uhhh, I didn't think this could actually happen", callbackName);
                        continue;
                    }

                    for (int j = 0; j < attribute.Names.Length; ++j)
                    {
                        ref var callbackData = ref CollectionsMarshal.GetValueRefOrAddDefault(callbackTargets, attribute.Names[j], out bool exist);
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

            Dictionary<string, TriggerData>? triggerData = null;
            TriggerValuesAttribute? triggerValuesAttribute = type.GetCustomAttribute<TriggerValuesAttribute>();

            if (triggerValuesAttribute != null)
            {
                triggerData = new Dictionary<string, TriggerData>();

                foreach (string triggerName in triggerValuesAttribute.Names)
                {
                    string styleTriggerName = JsonNamingPolicy.KebabCaseLower.ConvertName(triggerName);
                    if (TriggerValues.Triggers.TryGetValue(styleTriggerName, out ushort triggerMask))
                    {
                        TriggerData data = new TriggerData(triggerMask);
                        if (!triggerData.TryAdd(triggerName, data))
                        {
                            UILog.Logger?.Error("Duplicate trigger value defined '{n}'", triggerName);
                        }
                    }
                    else
                    {
                        UILog.Logger?.Error("Failed to find suitable trigger mask for '{n}'", triggerName);
                    }
                }
            }

            int currentPropertyIndex = 0;

            // generate a 'PropertyData' object for each serialized property
            Dictionary<string, PropertyData> propertyData = new Dictionary<string, PropertyData>();
            List<MemberInfo> memberInfoStack = new List<MemberInfo>();
            Type rootObjectType = type;

            ResolveMembersOnType(type, StateFlags.None, null);

            void ResolveMembersOnType(Type type, StateFlags parentStateFlags, string? parentName)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                Dictionary<string, object> setterDict = Enumerable.Concat(fields.Select(static (x) => (object)x), properties
                        .Where(static (x) => x.SetMethod != null)
                        .Select(static (x) => (object)x))
                    .ToDictionary(static (x) =>
                    {
                        if (x is FieldInfo fieldInfo)
                            return ConvertNameToPascalCase(fieldInfo.Name);
                        else if (x is PropertyInfo propertyInfo)
                            return ConvertNameToPascalCase(propertyInfo.Name);
                        throw new UnreachableException();
                    });

                foreach (var (name, fieldOrProperty) in setterDict)
                {
                    MemberInfo memberInfo = (MemberInfo)fieldOrProperty;
                    object[] attributes = memberInfo.GetCustomAttributes(true);

                    bool hasStyleInclude = Array.Exists(attributes, static (x) => x is StyleIncludeAttribute);
                    if (hasStyleInclude)
                    {
                        Type sourceType = (memberInfo is FieldInfo) ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;

                        string? styleName = null;
                        PropertyDataFlags dataFlags = PropertyDataFlags.None;
                        StateFlags stateFlags = StateFlags.None;
                        Type[]? converterTypes = null;

                        using RentedList<PropertyLink> links = new RentedList<PropertyLink>();

                        foreach (object opaqueAttrib in attributes)
                        {
                            if (opaqueAttrib is StyleSetupAttribute styleSetup)
                            {
                                if (styleSetup.IsGroup)
                                {
                                    if (parentName != null && !styleSetup.Flatten)
                                        styleName = $"{parentName}{styleName ?? name}";

                                    memberInfoStack.Add(memberInfo);

                                    ResolveMembersOnType(sourceType, styleSetup.StateFlags | parentStateFlags, styleSetup.Flatten ? parentName : styleName);

                                    memberInfoStack.RemoveAt(memberInfoStack.Count - 1);
                                    continue;
                                }
                                else
                                {
                                    stateFlags = styleSetup.StateFlags | parentStateFlags;
                                    styleName = styleSetup.AliasAs;
                                    converterTypes = styleSetup.ConverterTypes;
                                    dataFlags = styleSetup.IsEditable ? PropertyDataFlags.IsEditable : PropertyDataFlags.None;

                                    if (styleSetup.ConverterTypes != null && !Array.TrueForAll(styleSetup.ConverterTypes, (x) => x.IsAssignableTo(sourceType)))
                                    {
                                        UILog.Logger?.Error("Converter types specified for member '{m}' are not all assignable to base field type", name);
                                        converterTypes = null;
                                    }
                                }
                            }
                            else if (opaqueAttrib is StyleLink styleLink)
                            {
                                bool alreadyHasLink = false;
                                foreach (PropertyLink link in links)
                                {
                                    if (link.Name == styleLink.Name)
                                    {
                                        alreadyHasLink = true;
                                    }
                                }

                                if (alreadyHasLink)
                                {
                                    UILog.Logger?.Warning("Multiple links with same name on member '{m}'", name);
                                }
                                else if (setterDict.TryGetValue(styleLink.Name, out object? linkOpaqueMember))
                                {
                                    MemberInfo linkMember = (MemberInfo)linkOpaqueMember;
                                    Type linkSourceType = (linkMember is FieldInfo) ? ((FieldInfo)linkMember).FieldType : ((PropertyInfo)linkMember).PropertyType;
                                    
                                    using RentedList<MemberInfo> linkMemberStack = [.. memberInfoStack, linkMember];

                                    if (styleLink.Value == null)
                                    {
                                        if (linkSourceType.IsClass || IsTypeNullable(linkSourceType))
                                        {
                                            PropertyMethods propertyMethods = _manager.MethodGenerator.EmitMethods(rootObjectType, memberInfoStack.AsSpan());
                                            links.Add(new PropertyLink(styleLink.Name, propertyMethods, null));
                                        }
                                        else
                                        {
                                            UILog.Logger?.Error("Link value is not assignable to type in member because it is not nullable '{m}'", linkMember.Name);
                                        }
                                    }
                                    else
                                    {
                                        if (styleLink.Value.GetType().IsAssignableTo(linkSourceType))
                                        {
                                            PropertyMethods propertyMethods = _manager.MethodGenerator.EmitMethods(rootObjectType, memberInfoStack.AsSpan());
                                            links.Add(new PropertyLink(styleLink.Name, propertyMethods, styleLink.Value));
                                        }
                                        else
                                        {
                                            UILog.Logger?.Error("Link value is not assignable to type in member '{m}'", linkMember.Name);
                                        }
                                    }
                                }
                            }
                        }

                        ulong propertyMask = 0;
                        if (!dataFlags.HasFlags(PropertyDataFlags.IsEditable))
                        {
                            if (currentPropertyIndex >= 64)
                                throw new InvalidOperationException("Too many style properties defined");
                            propertyMask = (ulong)(1 << currentPropertyIndex++);
                        }

                        if (parentName != null)
                            styleName = $"{parentName}{styleName ?? name}";
                        else
                            styleName ??= name;

                        bool hasCallbacks = callbackTargets.TryGetValue(memberInfo.Name, out var callbackData);

                        memberInfoStack.Add(memberInfo);

                        PropertyData data = new PropertyData(
                            memberInfo.Name,
                            JsonNamingPolicy.KebabCaseLower.ConvertName(styleName),
                            dataFlags,
                            propertyMask,
                            stateFlags,
                            converterTypes ?? [sourceType],
                            fieldOrProperty,
                            _manager.MethodGenerator.EmitMethods(rootObjectType, memberInfoStack.AsSpan()),
                            hasCallbacks ? callbackData.Callbacks[..callbackData.Length] : null,
                            links.IsEmpty ? null : [.. links]);

                        propertyData.Add(styleName, data);
                        memberInfoStack.RemoveAt(memberInfoStack.Count - 1);
                    }
                }
            }

            if (callbackTargets.Count > 0)
            {
                foreach (var (key, (items, length)) in callbackTargets)
                {
                    Array.Clear(items, 0, length);
                    ArrayPool<string>.Shared.Return(items);
                }
            }

            return new WidgetCachedData(
                type,
                propertyData.ToFrozenDictionary(),
                triggerData?.ToFrozenDictionary() ?? FrozenDictionary<string, TriggerData>.Empty,
                callbackDelegates.ToFrozenDictionary(),
                constructorInfo);
        }

        public WidgetCachedData GetCachedData(Type type)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyledObject)));
            return _cachedData.GetOrAdd(type, CacheNewWidgetData);
        }

        private static string ConvertNameToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            int indexOfFirstWord = -1;
            for (int i = 0; i < name.Length; ++i)
            {
                if (char.IsLetterOrDigit(name[i]))
                {
                    indexOfFirstWord = i;
                    break;
                }
            }

            if (indexOfFirstWord == -1)
                return name;

            Span<char> charBuffer = stackalloc char[name.Length];

            bool capitalizeLetter = true;
            int bufferIndex = 0;

            for (int i = indexOfFirstWord; i < name.Length; ++i)
            {
                char c = name[i];
                if (!char.IsLetter(c))
                {
                    capitalizeLetter = true;
                    break;
                }

                if (capitalizeLetter)
                {
                    c = char.ToUpperInvariant(c);
                    capitalizeLetter = false;
                }
                else if (bufferIndex > 0 && char.IsUpper(c))
                {
                    if (char.IsUpper(charBuffer[bufferIndex - 1]))
                        c = char.ToLowerInvariant(c);
                }

                charBuffer[bufferIndex++] = c;
            }

            return charBuffer[..bufferIndex].ToString();
        }

        private static bool IsTypeNullable(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericType = type.GetGenericTypeDefinition();
                return genericType == typeof(Nullable<>) || genericType == typeof(LayoutValue<>);
            }

            return false;
        }

        public static WidgetPropertyCache Instance => UIManager.Instance.ReflectionManager.WidgetPropertyCache;
    }
}
