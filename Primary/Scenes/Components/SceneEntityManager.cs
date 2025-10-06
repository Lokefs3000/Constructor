using Arch.Core;
using Primary.Components;
using System.Runtime.CompilerServices;

namespace Primary.Scenes.Components
{
    public sealed unsafe class SceneEntityManager
    {
        private ComponentRegistry _registry;
        private ComponentDependencyGraph _dependencyGraph;

        internal SceneEntityManager()
        {
            _registry = new ComponentRegistry();
            _dependencyGraph = new ComponentDependencyGraph(_registry);
        }

        /// <inheritdoc cref="ComponentRegistry.RegisterComponentTemplated{T}"/>
        public void RegisterComponent<T>() where T : IComponent => _registry.RegisterComponentTemplated<T>();

        /// <inheritdoc cref="ComponentDependencyGraph.BuildDependencyGraph"/>
        public void RebuildDependencyGraph() => _dependencyGraph.BuildDependencyGraph();

        private void* AddComponentImpl(Entity entity, Type type)
        {
            ref readonly ComponentRegistryEntry entry = ref _registry.FindComponentEntry(type);
            if (Unsafe.IsNullRef(in entry))
            {
                return null;
            }

            void* pointer = entry.TryGetRefVoidImpl(entity);
            if (pointer == null)
            {
                //pointer = entity.AddAndGetVoidImpl(entity);

                ref readonly ComponentDependency dependency = ref _dependencyGraph.FindDependencyGraph(type);
                if (!Unsafe.IsNullRef(in dependency))
                {

                }
            }

            return pointer;
        }

        public event Action<SceneEntity>? EntityRenamed;
        public event Action<SceneEntity>? EntityEnabled;

        public event Action<SceneEntity>? EntityParentChange;
    }

    public enum EntityEvents : byte
    {
        /// <summary>
        /// <see cref="SceneEntityManager.EntityRenamed"/>
        /// <see cref="SceneEntityManager.EntityEnabled"/>
        /// </summary>
        EntityData = 0,

        /// <summary>
        /// <see cref="SceneEntityManager.EntityParentChange"/>
        /// </summary>
        EntityRelationship
    }
}
