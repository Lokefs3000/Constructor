using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Components;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Primary.Scenes
{
    public record struct SceneEntity : IEquatable<SceneEntity>
    {
        private Entity _entity;

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
            return ref SceneEntityManager.AddComponent<T>(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public void RemoveComponent<T>() where T : struct, IComponent
        {
            if (IsNull)
                throw new NullReferenceException();
            SceneEntityManager.RemoveComponent<T>(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public ref T GetComponent<T>() where T : struct, IComponent
        {
            if (IsNull)
                throw new NullReferenceException();
            return ref SceneEntityManager.GetComponent<T>(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public ref T SetComponent<T>(T value) where T : struct, IComponent
        {
            if (IsNull)
                throw new NullReferenceException();
            return ref SceneEntityManager.SetComponent<T>(ref this, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public IComponent? AddComponent(Type type)
        {
            if (IsNull)
                throw new NullReferenceException();
            return SceneEntityManager.AddComponent(ref this, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public void RemoveComponent(Type type)
        {
            if (IsNull)
                throw new NullReferenceException();
            SceneEntityManager.RemoveComponent(ref this, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public IComponent? GetComponent(Type type)
        {
            if (IsNull)
                throw new NullReferenceException();
            return SceneEntityManager.GetComponent(ref this, type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public IComponent? SetComponent(IComponent value, Type type)
        {
            if (IsNull)
                throw new NullReferenceException();
            return SceneEntityManager.SetComponent(ref this, value, type);
        }

        [IgnoreDataMember]
        public SceneEntity Parent
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
                SceneEntityManager.ChangeParent(ref this, value);
            }
        }

        public SceneEntityChildren Children
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
                return new SceneEntityChildren(_entity.Get<EntityRelationships>());
            }
        }

        public bool Enabled
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
#if DEBUG
                if (!_entity.Has<EntityEnabled>())
                    throw new NullReferenceException();
#endif
                _entity.Get<EntityEnabled>().Enabled = value;
            }
        }

        public string Name
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
#if DEBUG
                if (!_entity.Has<EntityName>())
                    throw new NullReferenceException();
#endif
                _entity.Get<EntityName>().Name = value;
            }
        }

        [IgnoreDataMember]
        public int SceneId
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

        public Scene Scene
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

        public SceneEntityComponents Components
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
        public bool IsSceneRoot
        {
            get
            {
                if (IsNull)
                    throw new NullReferenceException();
                return _entity.Has<Scene.SceneTagComponent>();
            }
        }

        public override string ToString() => IsNull ? "null" : Name;
        public override int GetHashCode() => _entity.GetHashCode();
        public bool Equals(SceneEntity entity) => entity._entity == _entity;

        [IgnoreDataMember]
        public Entity WrappedEntity => _entity;

        [IgnoreDataMember]
        public bool IsNull => _entity == Entity.Null || !_entity.IsAlive();

        public static readonly SceneEntity Null = new SceneEntity(Entity.Null);

        public static implicit operator SceneEntity(Entity entity) => new SceneEntity(entity);
        public static explicit operator Entity(SceneEntity sceneEntity) => sceneEntity.WrappedEntity;
    }
}
