using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.LowLevel.Jagged;
using Collections.Pooled;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering.Assets;
using Primary.Rendering.Partitioning;
using Primary.Threading;

namespace Primary.Rendering.Batching
{
    internal sealed class GridChunkBatcher : IJob
    {
        public BatchingManager? Manager;
        public BatchingOptions Options;
        public RenderList? RenderList;
        public RenderGridChunk? GridChunk;
        public bool DoFineFrustrumCulling;

        private readonly Dictionary<RenderMesh, RentedArray<Matrix4x4>> _batched;

        internal GridChunkBatcher()
        {
            _batched = new Dictionary<RenderMesh, RentedArray<Matrix4x4>>();
        }

        internal void ClearBatchData()
        {
            foreach (var (_, list) in _batched)
            {
                list.Dispose();
            }

            _batched.Clear();

            Manager = null;
            Options = default;
            RenderList = null;
            GridChunk = null;
            DoFineFrustrumCulling = default;
        }

        void IJob.Execute()
        {
            Guard.IsNotNull(Manager);
            Guard.IsNotNull(RenderList);
            Guard.IsNotNull(GridChunk);

            using RentedArray<Matrix4x4> matricies = new RentedArray<Matrix4x4>(GridChunk.EntityCount);
            int matrixIndex = 0;

            using (new ProfilingScope(DoFineFrustrumCulling ? "BatchChunkPartial" : "BatchChunkInside"))
            {
                foreach (var (renderMesh, list) in GridChunk.RedirectionLists)
                {
                    if (MaterialPass.GetShaderType(renderMesh.PassId) == Options.ShaderType)
                    {
                        matrixIndex = 0;

                        if (DoFineFrustrumCulling)
                        {
                            for (int i = 0; i < list.Count; ++i)
                            {
                                int renderableId = list[i];
                                ref Renderable renderable = ref GridChunk.GetRenderable(renderableId);

                                matricies[matrixIndex] = renderable.Transform;
                                matrixIndex += Unsafe.BitCast<bool, byte>((renderable.RenderLayers & Options.AllowedLayers) > 0 && renderable.Extents.IntersectAny(Options.ViewFrustrum));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < list.Count; ++i)
                            {
                                int renderableId = list[i];
                                ref Renderable renderable = ref GridChunk.GetRenderable(renderableId);

                                matricies[matrixIndex] = renderable.Transform;
                                matrixIndex += Unsafe.BitCast<bool, byte>((renderable.RenderLayers & Options.AllowedLayers) > 0);
                            }
                        }

                        if (matrixIndex > 0)
                        {
                            _batched.Add(renderMesh, new RentedArray<Matrix4x4>(matricies.Span[..matrixIndex]));
                        }
                    }
                }

                if (_batched.Count == 0)
                    Manager.ReturnGridChunkBatcher(this);
                else
                    RenderList.FinishBatching(this);
            }
        }

        public RODictionary<RenderMesh, RentedArray<Matrix4x4>> Batches => _batched;
    }
}
