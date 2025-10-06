using Primary.Components;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Scenes.Components
{
    public sealed class ComponentDependencyGraph
    {
        private readonly ComponentRegistry _registry;

        private Dictionary<Type, ComponentDependency> _dependencies;

        internal ComponentDependencyGraph(ComponentRegistry registry)
        {
            _registry = registry;

            _dependencies = new Dictionary<Type, ComponentDependency>();
        }

        /// <summary>Not thread-safe</summary>
        internal ref readonly ComponentDependency FindDependencyGraph(Type type) => ref CollectionsMarshal.GetValueRefOrNullRef(_dependencies, type);

        /// <summary>Not thread-safe</summary>
        internal void BuildDependencyGraph()
        {
            EngLog.Scene.Information("Rebuilding component dependency graph");

            _dependencies.Clear();
            foreach (ComponentRegistryEntry entry in _registry.Entries.Values)
            {
                ref ComponentDependency dependency = ref GetDependency(entry.Type);
                foreach (Attribute attribute in entry.Type.GetCustomAttributes())
                {
                    if (attribute is ComponentRequirementsAttribute requirements)
                    {
                        foreach (Type type in requirements.RequiredTypes)
                        {
                            ref readonly ComponentRegistryEntry registryEntry = ref _registry.FindComponentEntry(type);
                            if (Unsafe.IsNullRef(in registryEntry))
                            {
                                EngLog.Scene.Warning("Failed to find type: {t} required by component: {c}", type, entry.Type);
                            }
                            else
                            {
                                AddDependsOnConnection(in entry, in registryEntry);
                            }
                        }
                    }
                    else if (attribute is ComponentConnectionsAttribute connections)
                    {
                        foreach (Type type in connections.ConnectedTypes)
                        {
                            ref readonly ComponentRegistryEntry registryEntry = ref _registry.FindComponentEntry(type);
                            if (Unsafe.IsNullRef(in registryEntry))
                            {
                                EngLog.Scene.Warning("Failed to find type: {t} connected by component: {c}", type, entry.Type);
                            }
                            else
                            {
                                AddConnectedConnection(in entry, in registryEntry);
                            }
                        }
                    }
                }
            }

            ref ComponentDependency GetDependency(Type type)
            {
                ref ComponentDependency dependency = ref CollectionsMarshal.GetValueRefOrNullRef(_dependencies, type);
                if (Unsafe.IsNullRef(in dependency))
                {
                    ComponentDependency newDependency = new ComponentDependency(type.Assembly, new List<ComponentConnection>());
                    _dependencies.Add(type, newDependency);

                    dependency = ref CollectionsMarshal.GetValueRefOrNullRef(_dependencies, type);
                }

                Debug.Assert(!Unsafe.IsNullRef(in dependency));
                return ref dependency;
            }

            void AddDependsOnConnection(ref readonly ComponentRegistryEntry entry, ref readonly ComponentRegistryEntry required)
            {
                ref ComponentDependency currentGraph = ref GetDependency(entry.Type);
                ref ComponentDependency targetGraph = ref GetDependency(required.Type);

                currentGraph.ConnectionsList.Add(new ComponentConnection(required.Type, ConnectionType.DependsOn));
                targetGraph.ConnectionsList.Add(new ComponentConnection(entry.Type, ConnectionType.DependentOf));
            }

            void AddConnectedConnection(ref readonly ComponentRegistryEntry entry, ref readonly ComponentRegistryEntry required)
            {
                ref ComponentDependency currentGraph = ref GetDependency(entry.Type);

                currentGraph.ConnectionsList.Add(new ComponentConnection(required.Type, ConnectionType.Connected));
            }
        }
    }

    public readonly record struct ComponentDependency
    {
        private readonly Assembly _sourceAssembly;
        private readonly List<ComponentConnection> _connections;

        internal ComponentDependency(Assembly sourceAssembly, List<ComponentConnection> connections)
        {
            _sourceAssembly = sourceAssembly;
            _connections = connections;
        }

        public Assembly SourceAssembly => _sourceAssembly;
        public IReadOnlyList<ComponentConnection> Connections => _connections;

        internal List<ComponentConnection> ConnectionsList => _connections;
    }

    public readonly record struct ComponentConnection(Type Component, ConnectionType Type);

    public enum ConnectionType : byte
    {
        DependsOn = 0,
        DependentOf,
        Connected
    }
}
