using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Arch.Core;
using Primary.Components;
using TerraFX.Interop.Windows;

namespace Primary.Scenes.Entities
{
    internal sealed class ComponentReferences
    {
        private readonly Dictionary<ReferenceKey, ICRef> _references;
        private readonly Dictionary<ComponentType, ICRef> _nullReferences;

        internal ComponentReferences()
        {
            _references = new Dictionary<ReferenceKey, ICRef>();
            _nullReferences = new Dictionary<ComponentType, ICRef>();
        }

        internal void DestroyEntity(SceneEntity entity)
        {
            foreach (ComponentType type in entity.ComponentTypes)
            {
                ReferenceKey key = new ReferenceKey(entity, type);
                if (_references.Remove(key, out ICRef? cRef))
                {
                    cRef.DestroyInternal();
                }
            }
        }

        internal void DestroyComponent(SceneEntity entity, ComponentType type)
        {
            ReferenceKey key = new ReferenceKey(entity, type);
            if (_references.Remove(key, out ICRef? cRef))
            {
                cRef.DestroyInternal();
            }
        }

        internal CRef<T> CreateForComponent<T>(SceneEntity entity) where T : struct, IComponent
        {
            if (entity.HasComponent<T>())
            {
                ReferenceKey key = new ReferenceKey(entity, ComponentRegistry.TypeToComponentType[typeof(T)]);
                ref ICRef? cRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_references, key, out bool exists);

                if (!exists || cRef == null)
                {
                    cRef = new CRef<T>(entity);
                }

                Debug.Assert(cRef is CRef<T>);
                return Unsafe.As<CRef<T>>(cRef);
            }
            else
            {
                ref ICRef? cRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_nullReferences, ComponentRegistry.TypeToComponentType[typeof(T)], out bool exists);

                if (!exists || cRef == null)
                {
                    cRef = new CRef<T>();
                }

                Debug.Assert(cRef is CRef<T>);
                return Unsafe.As<CRef<T>>(cRef);
            }
        }

        private readonly record struct ReferenceKey(SceneEntity Entity, ComponentType Type) : IEquatable<ReferenceKey>
        {
            public override int GetHashCode() => HashCode.Combine(Entity, Type);
        }
    }
}
