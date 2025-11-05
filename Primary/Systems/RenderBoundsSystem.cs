using Arch.Core;
using Primary.Common;
using Primary.Components;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Timing;
using Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Systems
{
    public record struct RenderBoundsSystem : ISystem, IForEachWithEntity<WorldTransform, MeshRenderer, RenderBounds>
    {
        public void Schedule(World world, JobScheduler scheduler)
        {
            using (new ProfilingScope("Render bounds"))
            {
                world.InlineEntityQuery<RenderBoundsSystem, WorldTransform, MeshRenderer, RenderBounds>(s_query, ref this);
            }
        }

        public void Update(Entity e, ref WorldTransform world, ref MeshRenderer renderer, ref RenderBounds bounds)
        {
            if (renderer.Mesh == null)
                return;

            int frameIndex = Time.FrameIndex;
            if (world.UpdateIndex == frameIndex || renderer.UpdateIndex == frameIndex)
            {
                AABB local = renderer.Mesh.Boundaries;

                Vector3 c0 = Vector3.Transform(local.Minimum, world.Transformation);
                Vector3 c1 = Vector3.Transform(new Vector3(local.Maximum.X, local.Minimum.Y, local.Minimum.Z), world.Transformation);
                Vector3 c2 = Vector3.Transform(new Vector3(local.Minimum.X, local.Minimum.Y, local.Maximum.Z), world.Transformation);
                Vector3 c3 = Vector3.Transform(new Vector3(local.Maximum.X, local.Minimum.Y, local.Maximum.Z), world.Transformation);

                Vector3 c4 = Vector3.Transform(new Vector3(local.Minimum.X, local.Maximum.Y, local.Minimum.Z), world.Transformation);
                Vector3 c5 = Vector3.Transform(new Vector3(local.Maximum.X, local.Maximum.Y, local.Minimum.Z), world.Transformation);
                Vector3 c6 = Vector3.Transform(new Vector3(local.Minimum.X, local.Maximum.Y, local.Maximum.Z), world.Transformation);
                Vector3 c7 = Vector3.Transform(new Vector3(local.Maximum.X, local.Maximum.Y, local.Maximum.Z), world.Transformation);

                Vector3 absMin = Vector3.Min(c0, Vector3.Min(c1, Vector3.Min(c2, Vector3.Min(c3, Vector3.Min(c4, Vector3.Min(c5, Vector3.Min(c6, c7)))))));
                Vector3 absMax = Vector3.Max(c0, Vector3.Max(c1, Vector3.Max(c2, Vector3.Max(c3, Vector3.Max(c4, Vector3.Max(c5, Vector3.Max(c6, c7)))))));

                bounds.ComputedBounds = new AABB(absMin, absMax);
                bounds.UpdateIndex = frameIndex;
            }
        }

        private static readonly QueryDescription s_query = new QueryDescription().WithAll<WorldTransform, MeshRenderer, RenderBounds>();

        public ref readonly QueryDescription Description => ref s_query;
        public bool SystemNeedsFullExecutionTime => false;
    }
}
