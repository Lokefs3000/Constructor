using Arch.Core;
using Arch.Core.Extensions;
using Primary.Common;
using Primary.Components;
using Primary.Scripting;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Primary.Scenes.Components
{
    public sealed class SceneEntityManager
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private ComponentRegistry _registry;
        private ComponentDependencyGraph _dependencyGraph;
        private RelationshipManager _relationshipManager;
        private ComponentCallbacks _callbacks;

        private HashSet<SceneEntity> _entitiesToRemove;

        private EntityEvents _events;

        internal SceneEntityManager()
        {
            s_instance.Target = this;

            _registry = new ComponentRegistry();
            _dependencyGraph = new ComponentDependencyGraph(_registry);
            _relationshipManager = new RelationshipManager();
            _callbacks = new ComponentCallbacks();

            _entitiesToRemove = new HashSet<SceneEntity>();

            _events = EntityEvents.None;

            ScriptingManager scripting = Engine.GlobalSingleton.ScriptingManager;
            scripting.ContextUnloading += OnScriptContextUnloading;
        }

        private void OnScriptContextUnloading(ScriptContext context)
        {
            _callbacks.RemoveListenersFrom(context.LoadedAssemblies);
        }

        internal void HandleRemovedEntities()
        {
            if (_entitiesToRemove.Count > 0)
            {
                foreach (SceneEntity entity in _entitiesToRemove)
                {
                    EntityDestroyed?.Invoke(entity);
                    entity.Scene.World.Destroy(entity.WrappedEntity);
                }

                _entitiesToRemove.Clear();
            }
        }

        /// <inheritdoc cref="ComponentRegistry.RegisterComponentTemplated{T}"/>
        public void RegisterComponent<T>() where T : IComponent, new() => _registry.RegisterComponentTemplated<T>();

        /// <inheritdoc cref="ComponentDependencyGraph.BuildDependencyGraph"/>
        public void RebuildDependencyGraph() => _dependencyGraph.BuildDependencyGraph();

        /// <inheritdoc cref="ComponentCallbacks.AddComponentAddedCallback{T}(ComponentCallback)"/>
        public void AddComponentAddedCallback<T>(ComponentCallback callback) where T : IComponent, new() => _callbacks.AddComponentAddedCallback<T>(callback);
        /// <inheritdoc cref="ComponentCallbacks.RemoveComponentAddedCallback{T}(ComponentCallback)"/>
        public void RemoveComponentAddedCallback<T>(ComponentCallback callback) where T : IComponent, new() => _callbacks.RemoveComponentAddedCallback<T>(callback);

        /// <inheritdoc cref="ComponentCallbacks.AddComponentAddedCallback{T}(ComponentCallback)"/>
        public void AddComponentRemovedCallback<T>(ComponentCallback callback) where T : IComponent, new() => _callbacks.AddComponentRemovedCallback<T>(callback);
        /// <inheritdoc cref="ComponentCallbacks.RemoveComponentAddedCallback{T}(ComponentCallback)"/>
        public void RemoveComponentRemovedCallback<T>(ComponentCallback callback) where T : IComponent, new() => _callbacks.RemoveComponentRemovedCallback<T>(callback);

        private ref GenericComponent AddComponentImpl(ref readonly Entity entity, Type type)
        {
            ref readonly ComponentRegistryEntry entry = ref _registry.FindComponentEntry(type);
            if (Unsafe.IsNullRef(in entry))
            {
                return ref Unsafe.NullRef<GenericComponent>();
            }

            ref readonly ComponentDependency dependency = ref _dependencyGraph.FindDependencyGraph(type);
            if (Unsafe.IsNullRef(in dependency))
            {
                EngLog.Script.Error("Failed to find dependency graph for component: {c}", type);
                return ref Unsafe.NullRef<GenericComponent>();
            }

            ref GenericComponent comp = ref entry.TryGetRefImpl(in entity, out bool exists);
            if (!exists)
            {
                comp = ref entry.AddOrGetImpl(in entity);

                bool addedNewComponents = false;
                foreach (ComponentConnection connection in dependency.Connections)
                {
                    if (connection.Type == ConnectionType.DependsOn || connection.Type == ConnectionType.Connected)
                    {
                        AddComponentImpl(in entity, connection.Component);
                        addedNewComponents = true;
                    }
                }

                if (addedNewComponents)
                    comp = ref entry.TryGetRefImpl(in entity, out _);

                _callbacks.InvokeAdded(entity, type);

                if (Flags.HasFlag(_events, EntityEvents.EntityComponents))
                    ComponentAdded?.Invoke(entity, type);
            }

            return ref comp;
        }

        private bool RemoveComponentImpl(ref readonly Entity entity, Type type)
        {
            ref readonly ComponentRegistryEntry entry = ref _registry.FindComponentEntry(type);
            if (Unsafe.IsNullRef(in entry))
            {
                return false;
            }

            ref GenericComponent comp = ref entry.TryGetRefImpl(in entity, out bool exists);
            if (Unsafe.IsNullRef(in comp))
            {
                return false;
            }

            ref readonly ComponentDependency dependency = ref _dependencyGraph.FindDependencyGraph(type);
            if (Unsafe.IsNullRef(in dependency))
            {
                EngLog.Script.Error("Failed to find dependency graph for component: {c}", type);
                return false;
            }

            foreach (ComponentConnection connection in dependency.Connections)
            {
                if (connection.Type == ConnectionType.DependentOf)
                {
                    ref readonly ComponentRegistryEntry dependentEntry = ref _registry.FindComponentEntry(type);
                    if (Unsafe.IsNullRef(in entry))
                        continue;

                    dependentEntry.TryGetRefImpl(in entity, out exists);
                    if (exists)
                        return false;
                }
            }

            // TODO: improve so if other components also have this connection dont remove it until they also are
            foreach (ComponentConnection connection in dependency.Connections)
            {
                if (connection.Type == ConnectionType.Connected)
                {
                    if (!RemoveComponentImpl(in entity, connection.Component))
                    {
                        throw new Exception();
                    }
                }
            }

            _callbacks.InvokeRemoved(entity, type);

            if (Flags.HasFlag(_events, EntityEvents.EntityComponents))
                ComponentRemoved?.Invoke(entity, type);

            return true;
        }

        internal ref T AddComponent<T>(ref readonly Entity entity) where T : IComponent
        {
            return ref Unsafe.As<GenericComponent, T>(ref AddComponentImpl(in entity, typeof(T)));
        }

        internal bool RemoveComponent<T>(ref readonly Entity entity) where T : IComponent
        {
            return RemoveComponentImpl(in entity, typeof(T));
        }

        internal ref T SetComponent<T>(ref readonly Entity entity, T value)
        {
            ref T comp = ref entity.TryGetRef<T>(out bool exists);
            if (!exists)
                comp = ref Unsafe.As<GenericComponent, T>(ref AddComponentImpl(in entity, typeof(T)));

            comp = value;
            return ref comp;
        }

        internal IComponent? AddComponent(ref readonly Entity entity, Type type)
        {
            ref readonly ComponentRegistryEntry entry = ref _registry.FindComponentEntry(type);
            if (Unsafe.IsNullRef(in entry))
            {
                return null;
            }

            return entry.BoxImpl(ref AddComponentImpl(in entity, type));
        }

        internal bool RemoveComponent(ref readonly Entity entity, Type type)
        {
            return RemoveComponentImpl(in entity, type);
        }

        internal IComponent SetComponent(ref readonly Entity entity, Type type, IComponent value)
        {
            if (!entity.Has(type))
                AddComponentImpl(in entity, type);
            entity.Set(type, value);

            return value;
        }

        internal void ChangeEntityParent(Entity entity, Entity newParent)
        {
            if (_relationshipManager.SetParent(entity, newParent))
            {
                if (Flags.HasFlag(_events, EntityEvents.EntityRelationship))
                    EntityParentChange?.Invoke(entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetEntityName(Entity entity, string newName)
        {
            entity.Get<EntityName>().Name = newName;

            if (Flags.HasFlag(_events, EntityEvents.EntityData))
                EntityRenamed?.Invoke(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetEntityEnabled(Entity entity, bool isEnabled)
        {
            entity.Get<EntityEnabled>().Enabled = isEnabled;

            if (Flags.HasFlag(_events, EntityEvents.EntityData))
                EntityEnabled?.Invoke(entity);
        }

        internal Entity CreateReadyEntity(Scene scene)
        {
            Entity e = scene.World.Create(
                new EntityScene { SceneId = scene.Id },
                new EntityEnabled { Enabled = true },
                new EntityName { Name = "New SceneEntity" },
                new EntityRelationships { });

            AddComponent<Transform>(in e);

            EntityCreated?.Invoke(e);
            return e;
        }

        internal void DestroyEntity(Entity entity, bool dontRemoveFromParent = false)
        {
            _entitiesToRemove.Add(entity);

            if (!dontRemoveFromParent)
                ChangeEntityParent(entity, Entity.Null);

            ref EntityRelationships relationships = ref entity.TryGetRef<EntityRelationships>(out _);
            Debug.Assert(!Unsafe.IsNullRef(in relationships));

            if (relationships.Children.Count > 0)
            {
                foreach (Entity child in relationships.Children)
                {
                    DestroyEntity(entity, true);
                }
            }
        }

        public static EntityEvents Events { get => Instance._events; set => Instance._events = value; }

        public static event Action<SceneEntity>? EntityRenamed;
        public static event Action<SceneEntity>? EntityEnabled;

        public static event Action<SceneEntity>? EntityParentChange;

        public static event Action<SceneEntity, Type>? ComponentAdded;
        public static event Action<SceneEntity, Type>? ComponentRemoved;

        public static event Action<SceneEntity>? EntityCreated;
        public static event Action<SceneEntity>? EntityDestroyed;

        public static SceneEntityManager Instance => Unsafe.As<SceneEntityManager>(s_instance.Target!);
    }

    public enum EntityEvents : byte
    {
        None = 0,

        /// <summary>
        /// <see cref="SceneEntityManager.EntityRenamed"/>
        /// <see cref="SceneEntityManager.EntityEnabled"/>
        /// </summary>
        EntityData = 0,

        /// <summary>
        /// <see cref="SceneEntityManager.EntityParentChange"/>
        /// </summary>
        EntityRelationship,

        /// <summary>
        /// <see cref="SceneEntityManager.ComponentAdded"/>
        /// <see cref="SceneEntityManager.ComponentRemoved"/>
        /// </summary>
        EntityComponents
    }
}
