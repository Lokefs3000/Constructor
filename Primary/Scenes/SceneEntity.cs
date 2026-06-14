using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Components;
using Primary.Scenes.Components;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Primary.Scenes
{
    public readonly record struct SceneEntity : IEquatable<SceneEntity>, IEqualityComparer<SceneEntity>
    {
        private readonly Entity _entity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SceneEntity()
        {
            _entity = Entity.Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SceneEntity(Entity entity)
        {
            _entity = entity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public ref T AddComponent<T>() where T : struct, IComponent
        {
            if (IsNull)
                throw new NullReferenceException();
            return ref SceneEntityManager.Instance.AddComponent<T>(in _entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public bool RemoveComponent<T>() where T : struct, IComponent
        {
            if (IsNull)
                throw new NullReferenceException();
            return SceneEntityManager.Instance.RemoveComponent<T>(in _entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public ref T GetComponent<T>() where T : struct, IComponent
        {
            if (IsNull)
                throw new NullReferenceException();
            return ref _entity.TryGetRef<T>(out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public ref T SetComponent<T>(T value) where T : struct, IComponent
        {
            if (IsNull)
                throw new NullReferenceException();
            return ref SceneEntityManager.Instance.SetComponent(in _entity, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public IComponent? AddComponent(Type type)
        {
            if (IsNull)
                throw new NullReferenceException();
            return SceneEntityManager.Instance.AddComponent(in _entity, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public bool RemoveComponent(Type type)
        {
            if (IsNull)
                throw new NullReferenceException();
            return SceneEntityManager.Instance.RemoveComponent(in _entity, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public IComponent? GetComponent(Type type)
        {
            if (IsNull)
                throw new NullReferenceException();
            return _entity.Get(type) as IComponent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public IComponent? SetComponent(IComponent value, Type type)
        {
            if (IsNull)
                throw new NullReferenceException();

            return SceneEntityManager.Instance.SetComponent(in _entity, type, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy()
        {
            SceneEntityManager.Instance.DestroyEntity(_entity);
        }

        [IgnoreDataMember]
        public readonly SceneEntity Parent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
                return new SceneEntity(_entity.Get<EntityRelationships>().Parent);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsNull)
                    throw new NullReferenceException();
                SceneEntityManager.Instance.ChangeEntityParent(_entity, value.WrappedEntity);
            }
        }

        public readonly SceneEntityChildren Children
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
                return new SceneEntityChildren(_entity.Get<EntityRelationships>());
            }
        }

        public readonly bool Enabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
#if DEBUG
                if (!_entity.Has<EntityEnabled>())
                    throw new NullReferenceException();
#endif
                return _entity.Get<EntityEnabled>().Enabled;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsNull)
                    throw new NullReferenceException();
                SceneEntityManager.Instance.SetEntityEnabled(_entity, value);
            }
        }

        public readonly string Name
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
#if DEBUG
                if (!_entity.Has<EntityName>())
                    throw new NullReferenceException();
#endif
                return _entity.Get<EntityName>().Name;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsNull)
                    throw new NullReferenceException();
                SceneEntityManager.Instance.SetEntityName(_entity, value);
            }
        }

        [IgnoreDataMember]
        public readonly int SceneId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
#if DEBUG
                if (!_entity.Has<EntityScene>())
                    throw new NullReferenceException();
#endif
                return _entity.Get<EntityScene>().SceneId;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                throw new NotImplementedException();
            }
        }

        public readonly Scene Scene
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
#if DEBUG
                if (!_entity.Has<EntityScene>())
                    throw new NullReferenceException();
#endif
                return NullableUtility.ThrowIfNull(Engine.GlobalSingleton.SceneManager.FindScene(_entity.Get<EntityScene>().SceneId));
            }
            set => SceneId = value.Id;
        }

        public readonly SceneEntityComponents Components
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
                return new SceneEntityComponents(World.Worlds.DangerousGetReferenceAt(_entity.WorldId).GetEntityData(_entity));
            }
        }

        [IgnoreDataMember]
        public readonly ReadOnlySpan<ComponentType> ComponentTypes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
                return World.Worlds.DangerousGetReferenceAt(_entity.WorldId).GetEntityData(_entity).Archetype.Signature.Components;
            }
        }

        [IgnoreDataMember]
        public readonly bool IsSceneRoot
        {
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
                return _entity.Has<Scene.SceneTagComponent>();
            }
        }

        public readonly override string ToString() => IsNull ? "null" : Name;
        public readonly override int GetHashCode() => _entity.GetHashCode();
        public readonly bool Equals(SceneEntity entity) => entity._entity.Equals(_entity);

        public readonly bool Equals(SceneEntity x, SceneEntity y) => x._entity.Equals(y._entity);
        public readonly int GetHashCode([DisallowNull] SceneEntity obj) => obj._entity.GetHashCode();

        [IgnoreDataMember]
        public readonly Entity WrappedEntity => _entity;

        [IgnoreDataMember]
        public readonly bool IsNull => _entity == Entity.Null || !_entity.IsAlive();

        public static readonly SceneEntity Null = new SceneEntity(Entity.Null);

        public static implicit operator SceneEntity(Entity entity) => new SceneEntity(entity);
        public static explicit operator Entity(SceneEntity sceneEntity) => sceneEntity.WrappedEntity;
    }
}
