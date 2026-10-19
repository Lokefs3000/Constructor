using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Components;
using Primary.Scenes.Json;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Scenes.Components
{
    public sealed class ComponentRegistry
    {
        private readonly SceneEntityManager _entityManager;

        private readonly Dictionary<Type, ComponentRegistryEntry> _entries;
        private readonly Dictionary<Assembly, AssemblyList> _assemblies;

        private readonly Dictionary<string, Type> _entryTypeLookups;

        internal ComponentRegistry(SceneEntityManager entityManager)
        {
            _entityManager = entityManager;

            _entries = new Dictionary<Type, ComponentRegistryEntry>();
            _assemblies = new Dictionary<Assembly, AssemblyList>();

            _entryTypeLookups = new Dictionary<string, Type>();
        }

        /// <summary>Not thread-safe</summary>
        internal void RegisterComponentTemplated<T>() where T : IComponent, new()
        {
            Type type = typeof(T);
            if (type.IsClass)
            {
                Assembly assembly = type.Assembly;

                ComponentType componentType = Component.GetComponentType(type);

                _entries.Add(type, new ComponentRegistryEntry(type, componentType, RemoveTemplated<T>, AddClassTemplated<T>));

                string? fullName = SceneJsonSerializer.GetComponentKey(type);
                if (fullName != null)
                    _entryTypeLookups.Add(fullName, type);

                if (!_assemblies.TryGetValue(assembly, out AssemblyList list))
                {
                    list = new AssemblyList(new HashSet<Type>());
                    _assemblies.Add(assembly, list);
                }

                list.Types.Add(type);

                if (type.IsAssignableTo(typeof(EntityBehaviour)))
                    _entityManager.BehaviourManager.RegisterBehaviour(type);
            }
            else
            {
                Assembly assembly = type.Assembly;

                ComponentType componentType = Component.GetComponentType(type);

                _entries.Add(type, new ComponentRegistryEntry(type, componentType, TryGetRefTemplated<T>, AddOrGetTemplated<T>, BoxTemplated<T>, RemoveTemplated<T>));

                string? fullName = SceneJsonSerializer.GetComponentKey(type);
                if (fullName != null)
                    _entryTypeLookups.Add(fullName, type);

                if (!_assemblies.TryGetValue(assembly, out AssemblyList list))
                {
                    list = new AssemblyList(new HashSet<Type>());
                    _assemblies.Add(assembly, list);
                }

                list.Types.Add(type);
            }
        }

        private static ref GenericComponent TryGetRefTemplated<T>(ref readonly Entity e, out bool exists) where T : IComponent, new()
            => ref Unsafe.As<T, GenericComponent>(ref e.TryGetRef<T>(out exists));
        private static ref GenericComponent AddOrGetTemplated<T>(ref readonly Entity e) where T : IComponent, new()
            => ref Unsafe.As<T, GenericComponent>(ref e.AddOrGet<T>(new T()));
        private static IComponent BoxTemplated<T>(ref GenericComponent component) where T : IComponent, new()
           => (IComponent)Unsafe.As<GenericComponent, T>(ref component);
        private static void RemoveTemplated<T>(ref readonly Entity e) where T : IComponent, new()
        {
            e.Remove<T>();
        }
        private static IComponent AddClassTemplated<T>(ref readonly Entity e) where T : IComponent, new()
        {
            T added = new T();
            e.Add(added);
            return added;
        }

        /// <summary>Not thread-safe</summary>
        internal ref readonly ComponentRegistryEntry FindComponentEntry(Type type) => ref CollectionsMarshal.GetValueRefOrNullRef(_entries, type);

        internal Type? FindComponentFromFullName(string fullName) => _entryTypeLookups.TryGetValue(fullName, out Type? type) ? type : null;

        public RODictionary<Type, ComponentRegistryEntry> Entries => _entries;

        private readonly record struct AssemblyList(HashSet<Type> Types);
    }

    public readonly record struct ComponentRegistryEntry
    {
        public readonly Type Type;
        public readonly ComponentType NativeType;

        public readonly ComponentBehaviour Behaviour;

        public readonly TryGetRef? TryGetRefImpl;
        public readonly AddOrGet? AddOrGetImpl;
        public readonly Box? BoxImpl;
        public readonly Remove? RemoveImpl;
        public readonly AddClass? AddClassImpl;

        public ComponentRegistryEntry(Type type, ComponentType nativeType, TryGetRef tryGetRefImpl, AddOrGet addOrGetImpl, Box boxImpl, Remove removeImpl)
        {
            Type = type;
            NativeType = nativeType;

            ComponentUsageAttribute? usageAttribute = type.GetCustomAttribute<ComponentUsageAttribute>();
            Behaviour = new ComponentBehaviour(usageAttribute?.CanBeAdded ?? true);

            TryGetRefImpl = tryGetRefImpl;
            AddOrGetImpl = addOrGetImpl;
            BoxImpl = boxImpl;
            RemoveImpl = removeImpl;
        }

        public ComponentRegistryEntry(Type type, ComponentType nativeType, Remove remove, AddClass addClass)
        {
            Type = type;
            NativeType = nativeType;

            ComponentUsageAttribute? usageAttribute = type.GetCustomAttribute<ComponentUsageAttribute>();
            Behaviour = new ComponentBehaviour(usageAttribute?.CanBeAdded ?? true);

            RemoveImpl = remove;
            AddClassImpl = addClass;
        }

        public delegate ref GenericComponent TryGetRef(ref readonly Entity entity, out bool exists);
        public delegate ref GenericComponent AddOrGet(ref readonly Entity entity);
        public delegate IComponent Box(ref GenericComponent component);
        public delegate void Remove(ref readonly Entity entity);

        public delegate IComponent AddClass(ref readonly Entity entity);
    }

    public readonly record struct ComponentBehaviour(bool CanBeAdded);

    public readonly record struct GenericComponent : IComponent { }
}
