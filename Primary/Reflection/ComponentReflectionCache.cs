using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Components;
using Primary.Scenes;
using Primary.Scenes.Components;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace Primary.Reflection
{
    public sealed class ComponentReflectionCache
    {
        private ConcurrentDictionary<Assembly, AssemblyCacheList> _assemblyCaches;
        private ConcurrentDictionary<string, ComponentReflection> _componentCaches;

        private ComponentReflection _emptyCache;

        internal ComponentReflectionCache()
        {
            _assemblyCaches = new ConcurrentDictionary<Assembly, AssemblyCacheList>();
            _componentCaches = new ConcurrentDictionary<string, ComponentReflection>();

            _emptyCache = new ComponentReflection(null, Array.Empty<FieldInfo>());
        }

        internal void RemoveEntriesFromAssembly(Assembly assembly)
        {
            if (_assemblyCaches.TryRemove(assembly, out AssemblyCacheList cacheList))
            {
                foreach (var (key, _) in cacheList.Components)
                {
                    _componentCaches.TryRemove(key, out _);
                }
            }
        }

        public ComponentReflection GetReflection(string fullName)
        {
            if (_componentCaches.TryGetValue(fullName, out ComponentReflection reflectionCache))
                return reflectionCache;

            Type? type = SceneEntityManager.Instance.FindComponentFromFullName(fullName);
            if (type == null)
            {
                return _emptyCache;
            }
            else
            {
                using RentedList<FieldInfo> validFields = new RentedList<FieldInfo>();

                Type sourceType = type;
                do
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo field = fields[i];
                        if (field.GetCustomAttribute<IgnoreDataMemberAttribute>() == null)
                        {
                            validFields.Add(field);
                        }
                    }
                } while ((type = type.BaseType) != null && type.IsAssignableTo(typeof(IComponent)));

                ComponentReflection reflection = new ComponentReflection(sourceType, [.. validFields]);

                AssemblyCacheList cacheList = _assemblyCaches.GetOrAdd(sourceType.Assembly, static (x) => new AssemblyCacheList(new ConcurrentDictionary<string, ComponentReflection>()));
                if (cacheList.Components.TryAdd(fullName, reflection))
                    _componentCaches.TryAdd(fullName, reflection);

                return reflection;
            }
        }

        private readonly record struct AssemblyCacheList(ConcurrentDictionary<string, ComponentReflection> Components);
    }

    public readonly record struct ComponentReflection
    {
        private readonly Type? _component;
        private readonly Dictionary<string, FieldInfo> _fields;

        internal ComponentReflection(Type? component, FieldInfo[] fields)
        {
            _component = component;
            _fields = new Dictionary<string, FieldInfo>(fields.Length);

            for (int i = 0; i < fields.Length; i++)
            {
                _fields[fields[i].Name] = fields[i];
            }
        }

        public Type? Component => _component;
        public RODictionary<string, FieldInfo> Fields => _fields;
    }
}
