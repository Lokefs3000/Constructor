using Arch.Core;
using Primary.Components;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Primary.Scenes.Components
{
    public sealed class ComponentRegistry
    {
        private Dictionary<Type, ComponentRegistryEntry> _entries;

        internal ComponentRegistry()
        {
            _entries = new Dictionary<Type, ComponentRegistryEntry>();
        }

        /// <summary>Not thread-safe</summary>
        internal void RegisterComponentTemplated<T>() where T : IComponent
        {
            Type type = typeof(T);
            Assembly assembly = type.Assembly;

            ComponentType componentType = Component.GetComponentType(type);

            _entries.Add(type, new ComponentRegistryEntry(type, componentType, static (e) => null));
        }

        /// <summary>Not thread-safe</summary>
        internal ref readonly ComponentRegistryEntry FindComponentEntry(Type type) => ref CollectionsMarshal.GetValueRefOrNullRef(_entries, type);

        public IReadOnlyDictionary<Type, ComponentRegistryEntry> Entries => _entries;

        private readonly record struct AssemblyList(Dictionary<Type, ComponentRegistryEntry> Entries);
    }

    public readonly record struct ComponentRegistryEntry
    {
        public readonly Type Type;
        public readonly ComponentType NativeType;

        public readonly TryGetRefVoid TryGetRefVoidImpl;

        public ComponentRegistryEntry(Type type, ComponentType nativeType, TryGetRefVoid tryGetRefVoidImpl)
        {
            Type = type;
            NativeType = nativeType;

            TryGetRefVoidImpl = tryGetRefVoidImpl;
        }

        public unsafe delegate void* TryGetRefVoid(Entity entity);
    }
}
