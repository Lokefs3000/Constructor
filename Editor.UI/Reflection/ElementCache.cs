using CommunityToolkit.Diagnostics;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Serialization.Values;
using Editor.UI.Styling;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Editor.UI.Reflection
{
    public sealed class ElementCache
    {
        private ConcurrentDictionary<Type, CachedElementData> _cache;
        private HashSet<Assembly> _discovered;

        private ConcurrentDictionary<string, Type> _nameTraceDict;

        private Lock _discoveredLock;

        internal ElementCache()
        {
            _cache = new ConcurrentDictionary<Type, CachedElementData>();
            _discovered = new HashSet<Assembly>();

            _nameTraceDict = new ConcurrentDictionary<string, Type>();

            _discoveredLock = new Lock();
        }

        internal void DiscoverAssembly(Assembly assembly)
        {
            long timeStart = Stopwatch.GetTimestamp();

            ConcurrentDictionary<Type, bool> tempDict = new ConcurrentDictionary<Type, bool>();

            Type[] allTypes = assembly.GetTypes();
            Parallel.ForEach(allTypes, (t) =>
            {
                if (t.IsGenericType)
                    return;

                if ((t.IsAssignableTo(typeof(StyleBase)) && t != typeof(StyleBase)) || (t.IsAssignableTo(typeof(IUILayoutModifier)) && t != typeof(StyledBaseLayoutModifier)))
                {
                    if (ReflectType(t, out CachedElementData elementData))
                    {
                        _cache.TryAdd(t, elementData);
                        _nameTraceDict.TryAdd(elementData.PrettyName, t);

                        tempDict.TryAdd(t, true);
                    }
                }
            });

            _discovered.Add(assembly);

            UIManager.Logger?.Debug("Discovering elements in assembly: {asm} took: {secs:f3}s!", assembly.GetName().Name, Stopwatch.GetElapsedTime(timeStart).TotalSeconds);
        }

        private CachedElementData CacheValueFactory(Type type)
        {
            if (type.IsGenericType && type.IsGenericTypeDefinition)
            {
                using (_discoveredLock.EnterScope())
                {
                    if (_discovered.Contains(type.Assembly))
                        return default;

                    DiscoverAssembly(type.Assembly);
                    return _cache[type];
                }
            }

            if (ReflectType(type, out CachedElementData elementData))
                return elementData;
            return default;
        }

        public CachedElementData GetElementData(Type type)
        {
            Guard.IsTrue(!type.IsAbstract && (type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier))));

            return _cache.GetOrAdd(type, CacheValueFactory);

            if (_cache.TryGetValue(type, out CachedElementData value))
                return value;

            if (!_discovered.Contains(type.Assembly))
            {
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    if (ReflectType(type, out value))
                    {
                        _cache.TryAdd(type, value);
                        return value;
                    }
                    else
                        return default;
                }

                if (_discovered.Contains(type.Assembly))
                    return default;

                DiscoverAssembly(type.Assembly);
                return GetElementData(type);
            }

            return default;
        }

        public bool TryGetElementData(Type type, out CachedElementData value)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)) || type.IsAssignableTo(typeof(IUILayoutModifier)));

            if (!_cache.TryGetValue(type, out value))
            {
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    if (ReflectType(type, out value))
                    {
                        _cache.TryAdd(type, value);
                        return true;
                    }

                    return false;
                }
                else
                {
                    using (_discoveredLock.EnterScope())
                    {
                        if (_discovered.Contains(type.Assembly))
                            return false;

                        DiscoverAssembly(type.Assembly);
                        return _cache.TryGetValue(type, out value);
                    }
                }

                return false;
            }

            return value.PrettyName != null;

            if (!_cache.TryGetValue(type, out value))
            {
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    if (ReflectType(type, out value))
                    {
                        _cache.TryAdd(type, value);
                        return true;
                    }
                    else
                        return false;
                }

                if (_discovered.Contains(type.Assembly))
                    return false;

                DiscoverAssembly(type.Assembly);
                return TryGetElementData(type, out value);
            }

            return true;
        }

        public bool TryFindElementFromName(string prettyName, [NotNullWhen(true)] out Type? value)
        {
            return _nameTraceDict.TryGetValue(prettyName, out value);
        }

        public bool TryGetElementData(string prettyName, out CachedElementData value)
        {
            value = default;
            return TryFindElementFromName(prettyName, out Type? result) && TryGetElementData(result, out value);
        }

        public bool Exists(string prettyName) => _nameTraceDict.ContainsKey(prettyName);

        public IReadOnlyDictionary<Type, CachedElementData> Cache => _cache;

        private static bool ReflectType(Type type, out CachedElementData elementData)
        {
            elementData = default;

            bool isElement = type.IsAssignableTo(typeof(UIElement));

            string? prettyName = type.Name;
            {
                if (type.IsGenericType)
                {
                    prettyName = prettyName[..^2];
                }

                if (isElement)
                {
                    UIElementPrettyName? attrib = type.GetCustomAttribute<UIElementPrettyName>();
                    if (attrib != null && attrib.PrettyName != string.Empty)
                        prettyName = attrib.PrettyName;
                }
                else
                {
                    ModifierPrettyName? attrib = type.GetCustomAttribute<ModifierPrettyName>();
                    if (attrib != null && attrib.PrettyName != string.Empty)
                        prettyName = attrib.PrettyName;
                }
            }

            ConstructorInfo? constructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes);
            if (type.IsAssignableTo(typeof(UIElement)))
            {
                if (constructor == null)
                {
                    UIManager.Logger?.Error("Failed to properly cache element: {el} because it does not contain a public parameterless constructor", type);
                    return false;
                }
            }

            Type? customRoutine = null;
            {
                CustomSerilizationRoutineAttribute? attrib = type.GetCustomAttribute<CustomSerilizationRoutineAttribute>();
                if (attrib != null)
                    customRoutine = attrib.RoutineType;
            }

            string[] states = s_defaultState;
            {
                StyleableStatesAttribute? attribute = type.GetCustomAttribute<StyleableStatesAttribute>(true);
                if (attribute != null)
                {
                    states = attribute.States.Length == 0 ? s_defaultState : attribute.States;
                }
            }

            FrozenDictionary<string, int> stateMap = states.Select((x, i) => new KeyValuePair<string, int>(x, i - 1)).ToFrozenDictionary();
            FrozenDictionary<int, string> stateNameMap = states.Select((x, i) => new KeyValuePair<int, string>(i - 1, x)).ToFrozenDictionary();

            ElementStateData stateData = new ElementStateData(stateMap, stateNameMap, states);

            elementData = new CachedElementData(prettyName, constructor, type, customRoutine, stateData);
            return true;
        }

        private static readonly string[] s_defaultState = ["Normal"];
    }

    public readonly record struct CachedElementData(string PrettyName, ConstructorInfo? Constructor, Type TypeInfo, Type? CustomRoutine, ElementStateData StateData);
    public readonly record struct ElementStateData(FrozenDictionary<string, int> StateMap, FrozenDictionary<int, string> StateNameMap, string[] StateNames);
}
