using Arch.Core;
using Primary.Common;
using Primary.Components;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Scenes.Components;
using Schedulers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Systems
{
    public struct FlagTransformsSystem : ISystem, IDisposable, IForEachWithEntity<Transform>
    {
        public FlagTransformsSystem()
        {
            SceneEntityManager.Events |= EntityEvents.EntityRelationship;
            SceneEntityManager.EntityParentChange += OnEntityParentChanged;
        }

        public void Dispose()
        {
            SceneEntityManager.EntityParentChange -= OnEntityParentChanged;
        }

        private static void OnEntityParentChanged(SceneEntity entity)
        {
            ref Transform transform = ref entity.GetComponent<Transform>();
            if (!Unsafe.IsNullRef(in transform))
            {
                transform.InvalidFlags |= TransformInvalidFlags.Invalid | TransformInvalidFlags.Self | TransformInvalidFlags.AllChildren;
            }
        }

        public void Schedule(World world, JobScheduler scheduler)
        {
            using (new ProfilingScope("Flag transforms"))
            {
                world.InlineEntityQuery<FlagTransformsSystem, Transform>(s_query, ref this);
            }
        }

        public void Update(Entity entity, ref Transform transform)
        {
            if (Flags.HasFlag(transform.InvalidFlags, TransformInvalidFlags.Invalid))
            {
                SceneEntity sceneEntity = entity;
                while ((sceneEntity = sceneEntity.Parent) != SceneEntity.Null)
                {
                    ref Transform subTransform = ref sceneEntity.GetComponent<Transform>();
                    if (!Unsafe.IsNullRef(in subTransform))
                    {
                        if (Flags.HasFlag(subTransform.InvalidFlags, TransformInvalidFlags.Invalid))
                            break;

                        subTransform.InvalidFlags |= TransformInvalidFlags.Invalid;
                    }
                }
            }
        }

        private static readonly QueryDescription s_query = new QueryDescription().WithAll<Transform>();

        public ref readonly QueryDescription Description => ref s_query;
        public bool SystemNeedsFullExecutionTime => false;
    }
}
