using System.Numerics;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Components;
using Primary.Mathematics;
using Primary.Threading;
using Primary.Timing;

namespace Primary.Rendering.Tree.Jobs
{
    internal sealed class UpdateMeshRenderers
    {
        private readonly OctreeManager _manager;

        private readonly List<JobWorker> _workers;
        private readonly QueryDescription _queryDesc;

        internal UpdateMeshRenderers(OctreeManager octreeManager)
        {
            _manager = octreeManager;

            _workers = new List<JobWorker>();
            _queryDesc = new QueryDescription().WithAll<WorldTransform, RenderBounds, RenderOctantInfo, MeshRenderer>();
        }

        internal JobHandle ScheduleAll(JobHandle parentJob)
        {
            World world = Engine.GlobalSingleton.SceneManager.World;
            Query query = world.Query(in _queryDesc);

            using RentedList<JobHandle> jobs = new RentedList<JobHandle>(_workers.Count);

            int workerIndex = 0;
            foreach (Archetype archetype in query.GetArchetypeIterator())
            {
                RangePartitioner partioner = new RangePartitioner(JobScheduler.WorkerCount, archetype.ChunkCount);
                foreach (Arch.Core.Range range in partioner)
                {
                    JobWorker worker;
                    if (_workers.Count == workerIndex)
                    {
                        worker = new JobWorker(_manager);
                        _workers.Add(worker);

                        ++workerIndex;
                    }
                    else
                    {
                        worker = _workers[workerIndex++];
                    }

                    worker.Start = range.Start;
                    worker.Size = range.Length;
                    worker.Chunks = archetype.Chunks;

                    JobHandle handle = JobScheduler.Schedule(worker, parentJob, JobPriority.Realtime);

                    jobs.Add(handle);
                }
            }

            return jobs.Count == 1 ? jobs[0] : JobScheduler.CombineAll(jobs.AsSpan());
        }

        private sealed class JobWorker : IJob
        {
            private readonly OctreeManager _manager;

            public int Start;
            public int Size;
            public Chunk[]? Chunks;

            internal JobWorker(OctreeManager manager)
            {
                _manager = manager;
            }

            public void Execute()
            {
                ref Chunk chunk = ref Chunks!.DangerousGetReferenceAt(Start);

                long currentFrame = Time.FrameIndex;
                for (var chunkIndex = 0; chunkIndex < Size; chunkIndex++)
                {
                    ref Chunk currentChunk = ref Unsafe.Add(ref chunk, chunkIndex);

                    ref WorldTransform transformFirst = ref currentChunk.GetFirst<WorldTransform>();
                    ref RenderBounds renderBoundsFirst = ref currentChunk.GetFirst<RenderBounds>();
                    ref RenderOctantInfo octantInfoFirst = ref currentChunk.GetFirst<RenderOctantInfo>();
                    ref MeshRenderer meshRendererFirst = ref currentChunk.GetFirst<MeshRenderer>();

                    foreach (int entityIndex in currentChunk)
                    {
                        ref RenderOctantInfo octantInfo = ref Unsafe.Add(ref octantInfoFirst, entityIndex);
                        ref RenderBounds renderBounds = ref Unsafe.Add(ref renderBoundsFirst, entityIndex);

                        if (octantInfo.Octree != null && octantInfo.RenderableId != -1)
                        {
                            ref WorldTransform transform = ref Unsafe.Add(ref transformFirst, entityIndex);
                            ref MeshRenderer meshRenderer = ref Unsafe.Add(ref meshRendererFirst, entityIndex);

                            if (octantInfo.LastUpdateFrame < transform.UpdateIndex ||
                                octantInfo.LastUpdateFrame < renderBounds.UpdateIndex ||
                                octantInfo.LastUpdateFrame < meshRenderer.UpdateIndex)
                            {
                                RenderOctree octree = octantInfo.Octree!;

                                ref RenderableEntity renderable = ref octree.GetRenderable(octantInfo.RenderableId);
                                if (octantInfo.LastUpdateFrame < transform.UpdateIndex)
                                {
                                    renderable.Transform = transform.Transformation;
                                }

                                if (octantInfo.LastUpdateFrame < renderBounds.UpdateIndex)
                                {
                                    Vector3 center = (renderBounds.ComputedBounds.Minimum + renderBounds.ComputedBounds.Maximum) * 0.5f;
                                    Vector3 halfExtents = (renderBounds.ComputedBounds.Maximum - renderBounds.ComputedBounds.Minimum) * 0.5f;
                                    RenderExtents extents = new RenderExtents(center, halfExtents);

                                    renderable.Extents = extents;
                                    renderable.SqrBoundingRadius = halfExtents.LengthSquared();

                                    if (renderable.Octant != null)
                                    {
                                        RenderOctant octant = renderable.Octant;
                                        if (renderable.Octant.DoRenderExtentsFit(extents, out float excessSpace))
                                        {
                                            if (!octant.IsLeaf && excessSpace < RenderOctree.MaxOctantLoseness * 0.5f)
                                            {
                                                RenderOctant? childOctant = octree.FitExtentsInTree(extents, octant);
                                                if (childOctant != octant)
                                                {
                                                    octree.SetOctant(ref renderable, childOctant);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            RenderOctant? parentOctant = octree.FitExtentsInParentTree(extents, octant);
                                            if (parentOctant != octant)
                                            {
                                                octree.SetOctant(ref renderable, parentOctant);
                                            }
                                        }
                                    }
                                    else if (RenderOctree.CalculateExcessSpace(octree.SourceExtents, extents) < RenderOctree.MaxOctantLoseness * 0.5f)
                                    {
                                        RenderOctant? childOctant = octree.FitExtentsInTree(extents);
                                        if (childOctant != null)
                                        {
                                            octree.SetOctant(ref renderable, childOctant);
                                        }
                                    }
                                }

                                if (octantInfo.LastUpdateFrame < meshRenderer.UpdateIndex)
                                {
                                    renderable.Mesh = meshRenderer.Mesh;
                                    renderable.Material = meshRenderer.Material;
                                    renderable.Layers = meshRenderer.Layers;
                                }

                                octantInfo.LastUpdateFrame = currentFrame;
                            }
                        }
                    }
                }

                Chunks = null;
            }
        }
    }
}
