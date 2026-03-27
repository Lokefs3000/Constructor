using CommunityToolkit.Diagnostics;
using Editor.UI.Elements;
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
        private Dictionary<Type, CachedElementData> _cache;
        private HashSet<Assembly> _discovered;

        private Dictionary<string, Type> _nameTraceDict;

        internal ElementCache()
        {
            _cache = new Dictionary<Type, CachedElementData>();
            _discovered = new HashSet<Assembly>();

            _nameTraceDict = new Dictionary<string, Type>();
        }

        internal void DiscoverAssembly(Assembly assembly)
        {
            long timeStart = Stopwatch.GetTimestamp();

            ConcurrentDictionary<Type, CachedElementData> tempDict = new ConcurrentDictionary<Type, CachedElementData>();

            Type[] allTypes = assembly.GetTypes();
            Parallel.ForEach(allTypes, (t) =>
            {
                if (t.IsAssignableTo(typeof(UIElement)))
                {
                    string prettyName;
                    {
                        UIElementPrettyName? attrib = t.GetCustomAttribute<UIElementPrettyName>();
                        if (attrib != null && attrib.PrettyName != string.Empty)
                            prettyName = attrib.PrettyName;
                        else
                            prettyName = t.Name;
                    }

                    ConstructorInfo? constructor = t.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes);
                    if (constructor == null)
                    {
                        UIManager.Logger?.Error("Failed to properly cache element: {el} because it does not contain a public parameterless constructor", t);
                        return;
                    }

                    Type? customRoutine = null;
                    {
                        CustomSerilizationRoutineAttribute? attrib = t.GetCustomAttribute<CustomSerilizationRoutineAttribute>();
                        if (attrib != null)
                            customRoutine = attrib.RoutineType;
                    }

                    string[] states = s_defaultState;
                    {
                        UIElementStates? attribute = t.GetCustomAttribute<UIElementStates>(true);
                        if (attribute != null)
                        {
                            states = attribute.States.Length == 0 ? s_defaultState : attribute.States;
                        }
                    }

                    FrozenDictionary<string, int> stateMap = states.Select((x, i) => new KeyValuePair<string, int>(x, i - 1)).ToFrozenDictionary();
                    FrozenDictionary<int, string> stateNameMap = states.Select((x, i) => new KeyValuePair<int, string>(i - 1, x)).ToFrozenDictionary();

                    ElementStateData stateData = new ElementStateData(stateMap, stateNameMap, states);

                    tempDict.TryAdd(t, new CachedElementData(prettyName, constructor, t, customRoutine, stateData));
                }
            });

            _cache = _cache.Concat(tempDict).ToDictionary();
            _discovered.Add(assembly);

            foreach (var kvp in tempDict)
                _nameTraceDict.Add(kvp.Value.PrettyName, kvp.Key);

            UIManager.Logger?.Debug("Discovering elements in assembly: {asm} took: {secs:f3}s!", assembly.GetName().Name, Stopwatch.GetElapsedTime(timeStart).TotalSeconds);
        }

        public CachedElementData GetElementData(Type type)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)));

            if (_cache.TryGetValue(type, out CachedElementData value))
                return value;

            if (!_discovered.Contains(type.Assembly))
            {
                DiscoverAssembly(type.Assembly);
                return GetElementData(type);
            }

            return default;
        }

        public bool TryGetElementData(Type type, out CachedElementData value)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(StyleBase)));

            if (!_cache.TryGetValue(type, out value))
            {
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

        public IReadOnlyDictionary<Type, CachedElementData> Cache => _cache;

        private static readonly string[] s_defaultState = ["Normal"];
    }

    public readonly record struct CachedElementData(string PrettyName, ConstructorInfo Constructor, Type TypeInfo, Type? CustomRoutine, ElementStateData StateData);
    public readonly record struct ElementStateData(FrozenDictionary<string, int> StateMap, FrozenDictionary<int, string> StateNameMap, string[] StateNames);
}
