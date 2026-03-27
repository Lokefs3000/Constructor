using CommunityToolkit.Diagnostics;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Serialization.Values;
using Editor.UI.Styling;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Editor.UI.Reflection
{
    public sealed class ModifierCache
    {
        private Dictionary<Type, CachedModifierData> _cache;
        private HashSet<Assembly> _discovered;

        private Dictionary<string, Type> _nameTraceDict;

        internal ModifierCache()
        {
            _cache = new Dictionary<Type, CachedModifierData>();
            _discovered = new HashSet<Assembly>();

            _nameTraceDict = new Dictionary<string, Type>();
        }

        internal void DiscoverAssembly(Assembly assembly)
        {
            long timeStart = Stopwatch.GetTimestamp();

            ConcurrentDictionary<Type, CachedModifierData> tempDict = new ConcurrentDictionary<Type, CachedModifierData>();

            Type[] allTypes = assembly.GetTypes();
            Parallel.ForEach(allTypes, (t) =>
            {
                if (t.IsClass && t.IsAssignableTo(typeof(IUILayoutModifier)))
                {
                    string prettyName;
                    {
                        ModifierPrettyName? attrib = t.GetCustomAttribute<ModifierPrettyName>();
                        if (attrib != null && attrib.PrettyName != string.Empty)
                            prettyName = attrib.PrettyName;
                        else
                            prettyName = t.Name;
                    }

                    tempDict.TryAdd(t, new CachedModifierData(prettyName, t));
                }
            });

            _cache = _cache.Concat(tempDict).ToDictionary();
            _discovered.Add(assembly);

            foreach (var kvp in tempDict)
                _nameTraceDict.Add(kvp.Value.PrettyName, kvp.Key);

            UIManager.Logger?.Debug("Discovering modifiers in assembly: {asm} took: {secs:f3}s!", assembly.GetName().Name, Stopwatch.GetElapsedTime(timeStart).TotalSeconds);
        }

        public CachedModifierData GetModifierData(Type type)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(IUILayoutModifier)));

            if (_cache.TryGetValue(type, out CachedModifierData value))
                return value;

            if (!_discovered.Contains(type.Assembly))
            {
                DiscoverAssembly(type.Assembly);
                return GetModifierData(type);
            }

            return default;
        }

        public bool TryGetModifierData(Type type, out CachedModifierData value)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(IUILayoutModifier)));

            if (!_cache.TryGetValue(type, out value))
            {
                if (_discovered.Contains(type.Assembly))
                    return false;

                DiscoverAssembly(type.Assembly);
                return TryGetModifierData(type, out value);
            }

            return true;
        }

        public bool TryFindModifierFromName(string prettyName, [NotNullWhen(true)] out Type? value)
        {
            return _nameTraceDict.TryGetValue(prettyName, out value);
        }

        public bool TryGetModifierData(string prettyName, out CachedModifierData value)
        {
            value = default;
            return TryFindModifierFromName(prettyName, out Type? result) && TryGetModifierData(result, out value);
        }
    }

    public readonly record struct CachedModifierData(string PrettyName, Type TypeInfo);
}
