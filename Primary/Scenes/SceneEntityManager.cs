using Arch.Core;
using Arch.Core.Extensions;
using Primary.Common;
using Primary.Components;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Primary.Scenes
{
    public unsafe sealed class SceneEntityManager
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private World _world;

        //use a frozen dictionary instead?
        private Dictionary<Type, Component> _components;
        private ConcurrentDictionary<Entity, EntityRelationships> _relationships;

        private SemaphoreSlim _eventSemaphore;
        private Dictionary<Type, ComponentCallbacks> _callbacks;
        internal SceneEntityManager(World world)
        {
            _world = world;

            _components = new Dictionary<Type, Component>();
            _relationships = new ConcurrentDictionary<Entity, EntityRelationships>();

            _eventSemaphore = new SemaphoreSlim(1);
            _callbacks = new Dictionary<Type, ComponentCallbacks>();

            s_instance.Target = this;
        }

        internal SceneEntity CreateReadyEntity()
        {
            Entity entity = _world.Create();

            entity.Add(new EntityEnabled { Enabled = true }, new EntityName { Name = "New SceneEntity" });

            EntityRelationships relationships = new EntityRelationships();
            entity.Add(relationships);

            _relationships[entity] = relationships;
            return new SceneEntity(entity);
        }

        public static void BuildRequirementHierchies()
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);
            foreach (Type type in @this._components.Keys)
            {
                ref Component metadata = ref CollectionsMarshal.GetValueRefOrNullRef(@this._components, type);

                metadata.ExternalRequiredBy.Clear();
                metadata.ExternalRequiredBy.TrimExcess();
            }

            foreach (Type type in @this._components.Keys)
            {
                ref Component metadata = ref CollectionsMarshal.GetValueRefOrNullRef(@this._components, type);
                if (metadata.Requirements != null)
                {
                    foreach (Type requirement in metadata.Requirements.RequiredTypes)
                    {
                        ref Component subMetadata = ref CollectionsMarshal.GetValueRefOrNullRef(@this._components, requirement);
                        if (Unsafe.IsNullRef(ref subMetadata))
                        {
                            Log.Error("Failed to find required component type: {type} of: {rootType}", requirement, type);
                        }
                        else
                        {
                            subMetadata.ExternalRequiredBy.Add(type);
                        }
                    }
                }
            }
        }

        public static void Register<T>() where T : struct, IComponent
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);

            Type type = typeof(T);
            if (!type.IsAssignableTo(typeof(IComponent)))
                throw new InvalidDataException("Component must inherit from \"IComponent\"!");

            Component comp = new Component(
                type.GetCustomAttribute<ComponentRequirementsAttribute>(),
                type.GetCustomAttribute<ComponentConnectionsAttribute>(),
                type.GetCustomAttribute<ComponentUsageAttribute>(),
                ComponentRegistry.Add(type),
                static (Entity e, out bool exists) => Unsafe.AsPointer(ref e.TryGetRef<T>(out exists)),
                static (Entity e) => Unsafe.AsPointer(ref e.AddOrGet(new T())),
                static (Entity e, IComponent c) => e.Set<T>((T)c),
                static (Entity e) => e.Remove<T>(),
                static (void* c) => Unsafe.AsRef<T>(c),
                new HashSet<Type>(),
                type.Assembly);

            if (!@this._components.TryAdd(type, comp))
            {
                Log.Warning("Failed to register component type: {} because it is already registered.", type);
            }
        }

        public static ref T AddComponent<T>(ref SceneEntity sceneEntity) where T : struct, IComponent
            => ref Unsafe.AsRef<T>(AddComponent(typeof(T), ref sceneEntity));

        public static IComponent? AddComponent(ref SceneEntity sceneEntity, Type type)
        {
            void* ret = AddComponent(type, ref sceneEntity);
            if (ret == null)
                return null;

            ref Component metadata = ref CollectionsMarshal.GetValueRefOrNullRef(Unsafe.As<SceneEntityManager>(s_instance.Target!)._components, type);
            return metadata.Box(ret);
        }

        private static void* AddComponent(Type type, ref SceneEntity sceneEntity, bool forcedLocal = false)
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);

            ref Component metadata = ref CollectionsMarshal.GetValueRefOrNullRef(@this._components, type);
            if (Unsafe.IsNullRef(ref metadata))
            {
                Log.Error("No metadata for component type: {type}", type);
                return null;
            }

            if (!forcedLocal && metadata.Usage != null)
            {
                if (!metadata.Usage.CanBeAdded)
                    return null;
            }

            Entity entity = sceneEntity.WrappedEntity;

            void* component = metadata.TryGetRef(entity, out bool exists);
            if (!exists || component == null)
            {
                component = metadata.AddGet(entity);
                Debug.Assert(component != null);

                if (metadata.Connections != null)
                {
                    foreach (Type connection in metadata.Connections.ConnectedTypes)
                    {
                        if (AddComponent(connection, ref sceneEntity, true) == null)
                        {
                            Log.Error("Failed to satisfy connection: {conn} on component: {type}", connection, type);
                        }
                    }
                }

                if (metadata.Requirements != null)
                {
                    foreach (Type requirement in metadata.Requirements.RequiredTypes)
                    {
                        if (AddComponent(requirement, ref sceneEntity) == null)
                        {
                            Log.Error("Failed to satisfy requirment: {requ} on component: {type}", requirement, type);
                        }
                    }
                }

                component = metadata.TryGetRef(entity, out exists);
            }

            {
                @this._eventSemaphore.Wait();

                ref ComponentCallbacks callbacks = ref CollectionsMarshal.GetValueRefOrNullRef(@this._callbacks, type);
                if (!Unsafe.IsNullRef(ref callbacks) && callbacks.Added != null)
                {
                    for (int i = 0; i < callbacks.Added.Count; i++)
                    {
                        try
                        {
                            callbacks.Added[i](sceneEntity);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed invoking component removed callback on: {t}", type);
                        }
                    }
                }

                @this._eventSemaphore.Release();
            }

            return component;
        }

        public static ref T GetComponent<T>(ref SceneEntity sceneEntity) where T : struct, IComponent
        {
            return ref Unsafe.AsRef<T>(Unsafe.AsPointer(ref sceneEntity.WrappedEntity.TryGetRef<T>(out bool _)));
        }

        public static IComponent? GetComponent(ref SceneEntity sceneEntity, Type type)
        {
            sceneEntity.WrappedEntity.TryGet(type, out object? component);
            return component as IComponent;
        }

        public static bool RemoveComponent<T>(ref SceneEntity sceneEntity) where T : struct, IComponent
            => RemoveComponent(typeof(T), ref sceneEntity, false);

        public static bool RemoveComponent(ref SceneEntity sceneEntity, Type type)
            => RemoveComponent(type, ref sceneEntity, false);

        private static bool RemoveComponent(Type type, ref SceneEntity sceneEntity, bool forcedLocal = false)
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);

            ref Component metadata = ref CollectionsMarshal.GetValueRefOrNullRef(@this._components, type);
            if (Unsafe.IsNullRef(ref metadata))
            {
                Log.Error("No metadata for component type: {type}", type);
                return false;
            }

            if (!forcedLocal && metadata.Usage != null)
            {
                if (!metadata.Usage.CanBeAdded)
                    return false;
            }

            Entity entity = sceneEntity.WrappedEntity;

            metadata.TryGetRef(entity, out bool exists);
            if (!exists)
                return true;

            if (metadata.ExternalRequiredBy.Count > 0)
            {
                foreach (Type requiredBy in metadata.ExternalRequiredBy)
                {
                    ref Component subMetadata = ref CollectionsMarshal.GetValueRefOrNullRef(@this._components, requiredBy);
                    if (!Unsafe.IsNullRef(ref subMetadata))
                    {
                        subMetadata.TryGetRef(entity, out exists);
                        if (exists)
                        {
                            return false;
                        }
                    }
                }
            }

            if (metadata.Connections != null)
            {
                foreach (Type connection in metadata.Connections.ConnectedTypes)
                {
                    if (!RemoveComponent(connection, ref sceneEntity, true))
                    {
                        Log.Error("Failed to remove connected component: {conn} from: {type}", connection, type);
                    }
                }
            }

            {
                @this._eventSemaphore.Wait();

                ref ComponentCallbacks callbacks = ref CollectionsMarshal.GetValueRefOrNullRef(@this._callbacks, type);
                if (!Unsafe.IsNullRef(ref callbacks) && callbacks.Removed != null)
                {
                    for (int i = 0; i < callbacks.Removed.Count; i++)
                    {
                        try
                        {
                            callbacks.Removed[i](sceneEntity);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed invoking component removed callback on: {t}", type);
                        }
                    }
                }

                @this._eventSemaphore.Release();
            }

            metadata.Remove(entity);
            return true;
        }

        public static ref T SetComponent<T>(ref SceneEntity sceneEntity, T value) where T : struct, IComponent
            => ref Unsafe.AsRef<T>(SetComponent(typeof(T), ref sceneEntity, value));

        public static IComponent? SetComponent(ref SceneEntity sceneEntity, IComponent value, Type type)
        {
            void* ret = SetComponent(type, ref sceneEntity, value);
            if (ret == null)
                return null;

            ref Component metadata = ref CollectionsMarshal.GetValueRefOrNullRef(Unsafe.As<SceneEntityManager>(s_instance.Target!)._components, type);
            return metadata.Box(ret);
        }

        private static void* SetComponent(Type type, ref SceneEntity sceneEntity, IComponent newComponent)
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);

            ref Component metadata = ref CollectionsMarshal.GetValueRefOrNullRef(@this._components, type);
            if (Unsafe.IsNullRef(ref metadata))
            {
                Log.Error("No metadata for component type: {type}", type);
                return null;
            }

            Entity entity = sceneEntity.WrappedEntity;

            metadata.TryGetRef(entity, out bool exists);
            if (!exists)
                return null;

            metadata.Set(entity, newComponent);
            return metadata.TryGetRef(entity, out bool _);
        }

        internal static void ChangeParent(ref SceneEntity sceneEntity, SceneEntity parentEntity)
        {
            Entity child = sceneEntity.WrappedEntity;
            Entity parent = parentEntity.WrappedEntity;

            EntityRelationships childRelationships = child.Get<EntityRelationships>();
            EntityRelationships parentRelationships = parent.Get<EntityRelationships>();

            if (childRelationships.Parent != Entity.Null)
            {
                EntityRelationships oldParentRelationships = childRelationships.Parent.Get<EntityRelationships>();
                oldParentRelationships.Children.Remove(child);
            }

            childRelationships.Parent = parent;
            parentRelationships.Children.Add(child);
        }

        public static void AddComponentAddedCallback<T>(Action<SceneEntity> callback) where T : IComponent
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);
            Type type = typeof(T);

            @this._eventSemaphore.Wait();
            ref ComponentCallbacks callbacks = ref CollectionsMarshal.GetValueRefOrAddDefault(@this._callbacks, type, out bool exists);

            if (callbacks.Added == null)
                callbacks.Added = new List<Action<SceneEntity>>();

            callbacks.Added.Add(callback);
            @this._eventSemaphore.Release();
        }

        public static void RemoveComponentAddedCallback<T>(Action<SceneEntity> callback) where T : IComponent
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);
            Type type = typeof(T);

            @this._eventSemaphore.Wait();
            ref ComponentCallbacks callbacks = ref CollectionsMarshal.GetValueRefOrAddDefault(@this._callbacks, type, out bool exists);

            if (callbacks.Added == null)
                callbacks.Added = new List<Action<SceneEntity>>();

            callbacks.Added.Remove(callback);
            @this._eventSemaphore.Release();
        }

        public static void AddComponentRemovedCallback<T>(Action<SceneEntity> callback) where T : IComponent
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);
            Type type = typeof(T);

            @this._eventSemaphore.Wait();
            ref ComponentCallbacks callbacks = ref CollectionsMarshal.GetValueRefOrAddDefault(@this._callbacks, type, out bool exists);

            if (callbacks.Removed == null)
                callbacks.Removed = new List<Action<SceneEntity>>();

            callbacks.Removed.Add(callback);
            @this._eventSemaphore.Release();
        }

        public static void RemoveComponentRemovedCallback<T>(Action<SceneEntity> callback) where T : IComponent
        {
            SceneEntityManager @this = NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target);
            Type type = typeof(T);

            @this._eventSemaphore.Wait();
            ref ComponentCallbacks callbacks = ref CollectionsMarshal.GetValueRefOrAddDefault(@this._callbacks, type, out bool exists);

            if (callbacks.Removed == null)
                callbacks.Removed = new List<Action<SceneEntity>>();

            callbacks.Removed.Remove(callback);
            @this._eventSemaphore.Release();
        }

        private event ComponentEnabledCallback _componentEnabled;
        private event TransformChangedCallback _transformChanged;

        public static event ComponentEnabledCallback ComponentEnabled
        {
            add => NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target)._componentEnabled += value;
            remove => NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target)._componentEnabled -= value;
        }

        public static event TransformChangedCallback TransformChanged
        {
            add => NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target)._transformChanged += value;
            remove => NullableUtility.ThrowIfNull((SceneEntityManager?)s_instance.Target)._transformChanged -= value;
        }

        private readonly record struct Component(
            ComponentRequirementsAttribute? Requirements,
            ComponentConnectionsAttribute? Connections,
            ComponentUsageAttribute? Usage,
            ComponentType Type,
            TryGetRefWrapper TryGetRef,
            AddGetWrapper AddGet,
            SetWrapper Set,
            RemoveWrapper Remove,
            BoxWrapper Box,
            HashSet<Type> ExternalRequiredBy,
            Assembly OwningAssembly);

        private record struct ComponentCallbacks(
            List<Action<SceneEntity>>? Added,
            List<Action<SceneEntity>>? Removed);

        private unsafe delegate void* TryGetRefWrapper(Entity entity, out bool exists);
        private unsafe delegate void* AddGetWrapper(Entity entity);
        private unsafe delegate void SetWrapper(Entity entity, IComponent component);
        private unsafe delegate void RemoveWrapper(Entity entity);
        private unsafe delegate IComponent BoxWrapper(void* component);
    }

    public delegate void ComponentEnabledCallback(SceneEntity entity, bool enabled);
    public delegate void TransformChangedCallback(SceneEntity entity);
}
