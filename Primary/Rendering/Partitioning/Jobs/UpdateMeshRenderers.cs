using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Arch.Core;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Components;
using Primary.Mathematics;
using Primary.Rendering.Batching;
using Primary.Threading;
using Primary.Timing;

namespace Primary.Rendering.Partitioning.Jobs
{
    internal sealed class UpdateMeshRenderers
    {
        private readonly PartitioningManager _manager;

        private readonly QueryDescription _queryDesc;

        internal UpdateMeshRenderers(PartitioningManager manager)
        {
            _manager = manager;

            _queryDesc = new QueryDescription().WithAll<WorldTransform, RenderBounds, RenderOctantInfo, MeshRenderer>();
        }

        internal JobHandle ScheduleAll(JobHandle parentJob)
        {
            World world = Engine.GlobalSingleton.SceneManager.World;
            Query query = world.Query(in _queryDesc);

            using RentedList<JobHandle> jobs = new RentedList<JobHandle>();
            foreach (Archetype archetype in query.GetArchetypeIterator())
            {
                RangePartitioner partioner = new RangePartitioner(JobScheduler.WorkerCount, archetype.ChunkCount);
                foreach (Arch.Core.Range range in partioner)
                {
                    JobWorker worker = new JobWorker
                    {
                        Manager = _manager,
                        Start = range.Start,
                        Size = range.Length,
                        Chunks = archetype.Chunks
                    };

                    jobs.Add(JobScheduler.Schedule(worker, parentJob, JobPriority.Realtime, true));
                }
            }

            return JobScheduler.CombineAll(jobs.AsSpan());
        }

        private struct JobWorker : IJob
        {
            public PartitioningManager Manager;

            public int Start;
            public int Size;
            public Chunk[]? Chunks;

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
                        ref WorldTransform transform = ref Unsafe.Add(ref transformFirst, entityIndex);
                        ref RenderBounds renderBounds = ref Unsafe.Add(ref renderBoundsFirst, entityIndex);
                        ref RenderOctantInfo octantInfo = ref Unsafe.Add(ref octantInfoFirst, entityIndex);
                        ref MeshRenderer meshRenderer = ref Unsafe.Add(ref meshRendererFirst, entityIndex);

                        if (octantInfo.Chunk == null || octantInfo.RenderableId == -1)
                        {
                            if (!(meshRenderer.Mesh?.IsReady ?? false) || !(meshRenderer.Material?.IsReady ?? false))
                                continue;

                            Debug.Assert(octantInfo.Chunk == null && octantInfo.RenderableId == -1);

                            Int3 gridPoint = RenderGridChunk.GetGridPointFor(transform.Position);
                            RenderGridChunk gridChunk = Manager.GetOrCreateOctree(gridPoint);

                            Manager.RequestCreate(currentChunk.Entity(entityIndex), gridChunk);
                        }
                        else
                        {
                            ref Renderable renderable = ref octantInfo.Chunk.GetRenderable(octantInfo.RenderableId);

                            long updateFrame = octantInfo.LastUpdateFrame;
                            if (octantInfo.LastUpdateFrame < transform.UpdateIndex)
                            {
                                Int3 gridPoint = RenderGridChunk.GetGridPointFor(transform.Position);
                                if (gridPoint != octantInfo.Chunk.GridPoint)
                                {
                                    RenderGridChunk gridChunk = Manager.GetOrCreateOctree(gridPoint);

                                    Manager.RequestDestroy(octantInfo.Chunk, octantInfo.RenderableId);
                                    Manager.RequestCreate(currentChunk.Entity(entityIndex), gridChunk);

                                    octantInfo.Chunk = null;
                                    octantInfo.RenderableId = -1;
                                    continue;
                                }
                                else
                                {
                                    renderable.Transform = transform.Transformation;
                                }

                                updateFrame = currentFrame;
                            }

                            if (octantInfo.LastUpdateFrame < renderBounds.UpdateIndex)
                            {
                                renderable.Extents = new RenderExtents(renderBounds.ComputedBounds);
                                octantInfo.Chunk.FitExtents(renderable.Extents);

                                updateFrame = currentFrame;
                            }

                            if (octantInfo.LastUpdateFrame < meshRenderer.UpdateIndex)
                            {
                                // No longer able to participate in rendering so we remove it
                                if (!(meshRenderer.Mesh?.IsReady ?? false) || !(meshRenderer.Material?.IsReady ?? false))
                                {
                                    Manager.RequestDestroy(octantInfo.Chunk, octantInfo.RenderableId);

                                    octantInfo.Chunk = null;
                                    octantInfo.RenderableId = -1;
                                    continue;
                                }

                                bool needsStructureUpdate = false;
                                if (renderable.MeshIndex != meshRenderer.Mesh.MeshIndex)
                                {
                                    Debug.Assert(renderable.ModelIndex == meshRenderer.Mesh.ParentAsset!.UniqueIndex);
                                    needsStructureUpdate = true;
                                }
                                else
                                {
                                    for (int i = 0; i < renderable.MaterialPasses.Count; ++i)
                                    {
                                        ref RenderablePassIndex passIndex = ref renderable.MaterialPasses[i];
                                        if (passIndex.ArrayIndex != -1)
                                        {
                                            if (MaterialPass.GetMaterialIndex(passIndex.PassId) != meshRenderer.Material.UniqueIndex)
                                            {
                                                needsStructureUpdate = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (needsStructureUpdate)
                                {
                                    Manager.RequestUpdate(currentChunk.Entity(entityIndex), octantInfo.Chunk, octantInfo.RenderableId);
                                }

                                renderable.RenderLayers = meshRenderer.Layers;

                                updateFrame = currentFrame;
                            }

                            octantInfo.LastUpdateFrame = updateFrame;
                        }
                    }
                }

                Chunks = null;
            }
        }
    }
}
