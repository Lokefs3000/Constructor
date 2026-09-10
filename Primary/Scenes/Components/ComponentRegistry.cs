using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Components;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Scenes.Components
{
    public sealed class ComponentRegistry
    {
        private Dictionary<Type, ComponentRegistryEntry> _entries;
        private Dictionary<Assembly, AssemblyList> _assemblies;

        internal ComponentRegistry()
        {
            _entries = new Dictionary<Type, ComponentRegistryEntry>();
            _assemblies = new Dictionary<Assembly, AssemblyList>();
        }

        /// <summary>Not thread-safe</summary>
        internal void RegisterComponentTemplated<T>() where T : IComponent, new()
        {
            Type type = typeof(T);
            Assembly assembly = type.Assembly;

            ComponentType componentType = Component.GetComponentType(type);

            _entries.Add(type, new ComponentRegistryEntry(type, componentType, TryGetRefTemplated<T>, AddOrGetTemplated<T>, BoxTemplated<T>));

            if (!_assemblies.TryGetValue(assembly, out AssemblyList list))
            {
                list = new AssemblyList(new HashSet<Type>());
                _assemblies.Add(assembly, list);
            }

            list.Types.Add(type);
        }

        private static ref GenericComponent TryGetRefTemplated<T>(ref readonly Entity e, out bool exists) where T : IComponent, new()
            => ref Unsafe.As<T, GenericComponent>(ref e.TryGetRef<T>(out exists));
        private static ref GenericComponent AddOrGetTemplated<T>(ref readonly Entity e) where T : IComponent, new()
            => ref Unsafe.As<T, GenericComponent>(ref e.AddOrGet<T>(new T()));
        private static IComponent BoxTemplated<T>(ref GenericComponent component) where T : IComponent, new()
           => component;

        /// <summary>Not thread-safe</summary>
        internal ref readonly ComponentRegistryEntry FindComponentEntry(Type type) => ref CollectionsMarshal.GetValueRefOrNullRef(_entries, type);

        public RODictionary<Type, ComponentRegistryEntry> Entries => _entries;

        private readonly record struct AssemblyList(HashSet<Type> Types);
    }

    public readonly record struct ComponentRegistryEntry
    {
        public readonly Type Type;
        public readonly ComponentType NativeType;

        public readonly ComponentBehaviour Behaviour;

        public readonly TryGetRef TryGetRefImpl;
        public readonly AddOrGet AddOrGetImpl;
        public readonly Box BoxImpl;

        public ComponentRegistryEntry(Type type, ComponentType nativeType, TryGetRef tryGetRefImpl, AddOrGet addOrGetImpl, Box boxImpl)
        {
            Type = type;
            NativeType = nativeType;

            ComponentUsageAttribute? usageAttribute = type.GetCustomAttribute<ComponentUsageAttribute>();
            Behaviour = new ComponentBehaviour(usageAttribute?.CanBeAdded ?? true);

            TryGetRefImpl = tryGetRefImpl;
            AddOrGetImpl = addOrGetImpl;
            BoxImpl = boxImpl;
        }

        public delegate ref GenericComponent TryGetRef(ref readonly Entity entity, out bool exists);
        public delegate ref GenericComponent AddOrGet(ref readonly Entity entity);
        public delegate IComponent Box(ref GenericComponent component);
    }

    public readonly record struct ComponentBehaviour(bool CanBeAdded);

    public readonly record struct GenericComponent : IComponent { }
}
