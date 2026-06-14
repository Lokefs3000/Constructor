using Arch.Core;
using Arch.Core.Extensions;
using Primary.Common;
using Primary.Components;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Scenes.Components;
using Primary.Timing;
using Schedulers;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Systems
{
    public struct TransformSystem : ISystem
    {
        public void Schedule(World world, JobScheduler scheduler)
        {
            using (new ProfilingScope("Transform"))
            {
                SceneManager manager = Engine.GlobalSingleton.SceneManager;
                IReadOnlyList<Scene> scenes = manager.Scenes;

                int frameIndex = Time.FrameIndex;

                for (int i = 0; i < scenes.Count; i++)
                {
                    Scene scene = scenes[i];

                    ref Transform tr = ref scene.Root.WrappedEntity.Get<Transform>();
                    if (Flags.HasFlag(tr.InvalidFlags, TransformInvalidFlags.Invalid))
                    {
                        IterateThroughHierchy(scene.Root.WrappedEntity, Matrix4x4.Identity, frameIndex, false);
                        tr.InvalidFlags = TransformInvalidFlags.None;
                    }
                }
            }
        }

        private void IterateThroughHierchy(SceneEntity e, Matrix4x4 parent, int frameIndex, bool allChildren)
        {
            foreach (var child in e.Children)
            {
                Entity childEntity = child.WrappedEntity;

                ref Transform tr = ref childEntity.Get<Transform>();
                if (Flags.HasFlag(tr.InvalidFlags, TransformInvalidFlags.Invalid) || allChildren)
                {
                    ref WorldTransform wt = ref childEntity.Get<WorldTransform>();
                    ref LocalTransform lt = ref childEntity.Get<LocalTransform>();

                    if (!Unsafe.IsNullRef(ref wt) && !Unsafe.IsNullRef(ref lt))
                    {
                        if (Flags.HasFlag(tr.InvalidFlags, TransformInvalidFlags.Self))
                        {
                            lt.Transformation =
                                    Matrix4x4.CreateScale(tr.Scale) *
                                    Matrix4x4.CreateFromQuaternion(tr.Rotation) *
                                    Matrix4x4.CreateTranslation(tr.Position);
                            lt.UpdateIndex = frameIndex;
                        }

                        wt.Transformation = lt.Transformation * parent;
                        wt.UpdateIndex = frameIndex;

                        if (!child.Children.IsEmpty)
                            IterateThroughHierchy(childEntity, wt.Transformation, frameIndex, Flags.HasFlag(tr.InvalidFlags, TransformInvalidFlags.AllChildren));
                    }
                    else
                    {
                        IterateThroughHierchy(childEntity, parent, frameIndex, Flags.HasFlag(tr.InvalidFlags, TransformInvalidFlags.AllChildren));
                    }

                    tr.InvalidFlags = TransformInvalidFlags.None;
                }
            }
        }

        public ref readonly QueryDescription Description => ref QueryDescription.Null;
        public bool SystemNeedsFullExecutionTime => true;
    }
}
