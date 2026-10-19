using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.Extensions;
using Primary.Collections.ReadOnly;
using Primary.Memory;
using Primary.Memory.Native;
using Primary.Rendering.Datatypes;
using Primary.Rendering.Passes;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI;

namespace Primary.Rendering
{
    public sealed class CompiledRenderPlan
    {
        private readonly DescriptorAllocator _directDescriptorAllocator;
        private readonly DescriptorAllocator _computeDescriptorAllocator;

        private readonly List<RHISwapChain> _swapChains;

        private readonly List<CompiledRenderPass> _renderPasses;
        private readonly List<RenderEventInfo> _eventInfos;

        private readonly List<RenderResourceInfo> _resourceInfos;
        private int[] _resourceIndices;
        private readonly List<GenericResourceDesc> _resourceDescs;
        private readonly List<RenderUploadInfo> _uploadInfos;

        private int _totalResourceCount;

        private int _requiredResourceMemory;
        private int _requiredUploadMemory;

        private readonly List<RenderBarrier> _warmupBarriers;

        internal CompiledRenderPlan(FrameGraph frameGraph)
        {
            _directDescriptorAllocator = new DescriptorAllocator(
                frameGraph.Options.DescriptorRTHeapSize,
                frameGraph.Options.DescriptorDSHeapSize,
                frameGraph.Options.DescriptorRVCPUHeapSize,
                frameGraph.Options.DescriptorRVGPUHeapSize,
                frameGraph.Options.DescriptorRVSamplerHeapSize);

            _computeDescriptorAllocator = new DescriptorAllocator(
                frameGraph.Options.DescriptorRTHeapSize,
                frameGraph.Options.DescriptorDSHeapSize,
                frameGraph.Options.DescriptorRVCPUHeapSize,
                frameGraph.Options.DescriptorRVGPUHeapSize,
                frameGraph.Options.DescriptorRVSamplerHeapSize);

            _swapChains = new List<RHISwapChain>();

            _renderPasses = new List<CompiledRenderPass>();
            _eventInfos = new List<RenderEventInfo>();

            _resourceInfos = new List<RenderResourceInfo>();
            _resourceIndices = [];
            _resourceDescs = new List<GenericResourceDesc>();
            _uploadInfos = new List<RenderUploadInfo>();

            _totalResourceCount = 0;

            _requiredResourceMemory = 0;
            _requiredUploadMemory = 0;

            _warmupBarriers = new List<RenderBarrier>();
        }

        internal void ClearPlanForPooling()
        {
            _directDescriptorAllocator.ClearDescriptorData();
            _computeDescriptorAllocator.ClearDescriptorData();

            _swapChains.Clear();

            _renderPasses.Clear();
            _eventInfos.Clear();

            _resourceInfos.Clear();
            _resourceDescs.Clear();
            _uploadInfos.Clear();

            _totalResourceCount = 0;

            _requiredResourceMemory = 0;
            _requiredUploadMemory = 0;

            _warmupBarriers.Clear();
        }

        internal ref readonly GenericResourceDesc GetDescription(int resourceIndex) => ref _resourceDescs.GetRefAtIndex(resourceIndex);

        internal void AddSwapChain(RHISwapChain swapChain)
        {
            _swapChains.Add(swapChain);
        }

        internal void AddRenderPass(CompiledRenderPass renderPass)
        {
            _renderPasses.Add(renderPass);
        }

        internal void AddEvent(RenderEventType eventType, CompiledRenderPass? renderPass1 = null, CompiledRenderPass? renderPass2 = null)
        {
            _eventInfos.Add(new RenderEventInfo(eventType, renderPass1, renderPass2));
        }

        internal void AllocateResourceIndices(int count, bool copyIfResized = false)
        {
            if (_resourceIndices.Length < count)
            {
                int[] newArray = new int[MemoryHelper.AlignUp(count, 16)];

                if (copyIfResized)
                {
                    Array.Copy(_resourceIndices, newArray, _resourceIndices.Length);
                }

                _resourceIndices = newArray;
            }

#if DEBUG
            if (!copyIfResized)
                Array.Fill(_resourceIndices, -1, 0, count);
#endif
        }

        internal void SetResourceIndex(int resourceIndex, int realIndex)
        {
            _resourceIndices[resourceIndex] = realIndex;
        }

        internal void AddResource(int byteOffset, int sizeInBytes, int resourceIndex, GenericResourceDesc resourceDesc)
        {
            SetResourceIndex(resourceIndex, _resourceInfos.Count);
            _resourceInfos.Add(new RenderResourceInfo(byteOffset, sizeInBytes, resourceIndex, _resourceDescs.Count, null));
            _resourceDescs.Add(resourceDesc);
        }

        internal void AddResource(RHIResource resource, int resourceIndex)
        {
            SetResourceIndex(resourceIndex, _resourceInfos.Count);
            _resourceInfos.Add(new RenderResourceInfo(-1, -1, resourceIndex, -1, resource));
        }

        internal void AddUpload(int byteOffsetInHeap, ArrayPtr<byte> sourceData, uint destSlice, FGBox destBox, int resourceIndex)
        {
            _uploadInfos.Add(new RenderUploadInfo(byteOffsetInHeap, sourceData, destSlice, destBox, resourceIndex));
        }

        internal void AddWarmupBarrier(RenderBarrier barrier) => _warmupBarriers.Add(barrier);
        internal void AddWarmupBarrier(ReadOnlySpan<RenderBarrier> barriers) => _warmupBarriers.AddRange(barriers);

        internal CompiledRenderPass FindRenderPass(RenderPassIndex renderPass) => _renderPasses[renderPass.Index];

        internal DescriptorAllocator DirectDescriptorAllocator => _directDescriptorAllocator;
        internal DescriptorAllocator ComputeDescriptorAllocator => _computeDescriptorAllocator;

        internal ROList<RHISwapChain> SwapChains => _swapChains;
        public ROList<CompiledRenderPass> RenderPasses => _renderPasses;
        internal ROList<RenderEventInfo> Events => _eventInfos;
        internal ROList<RenderResourceInfo> Resources => _resourceInfos;
        internal ReadOnlySpan<int> ResourceIndices => _resourceIndices;
        internal ROList<GenericResourceDesc> ResourceDescs => _resourceDescs;
        internal ROList<RenderUploadInfo> Uploads => _uploadInfos;

        internal ROList<RenderBarrier> WarmupBarriers => _warmupBarriers;

        public int TotalResourceCount { get => _totalResourceCount; internal set => _totalResourceCount = value; }

        public int RequiredResourceMemory { get => _requiredResourceMemory; internal set => _requiredResourceMemory = value; }
        public int RequiredUploadMemory { get => _requiredUploadMemory; internal set => _requiredUploadMemory = value; }
    }

    internal readonly record struct RenderEventInfo(RenderEventType EventType, CompiledRenderPass? RenderPass1 = null, CompiledRenderPass? RenderPass2 = null);
    internal readonly record struct RenderResourceInfo(int ByteOffset, int SizeInBytes, int ResourceIndex, int DescriptionIndex, RHIResource? ImportedResource);
    internal readonly record struct RenderUploadInfo(int ByteOffsetInHeap, ArrayPtr<byte> SourceData, uint DestSlice, FGBox DestBox, int ResourceIndex);

    internal enum RenderEventType : byte
    {
        // RenderPass1 is not null
        ExecuteDirect,

        // RenderPass1 is not null
        ExecuteCompute,

        // RenderPass1 and RenderPass2 are not null
        InsertBarrier
    }

    internal readonly record struct ResourceBarrierInfo(ResourceHandle Resource, ResourceBarrierSync Sync, ResourceBarrierAccess Access, BarrierQueueSpec QueueSpec);

    public enum ResourceBarrierSync : byte
    {
        None = 0,
        All,
        AllShading,

        Draw,
        Indirect,
        IndexInput,

        DepthStencil,
        RenderTarget,

        PixelShading,
        VertexShading,
        ComputeShading,
        Raytracing,

        Copy,
        Resolve,

        BuildRTAcceleration,
        CopyRTAcceleration,

        ClearUnorderedAccess,

        Split
    }

    public enum ResourceBarrierAccess : byte
    {
        Common = 0,
        None,

        VertexBuffer,
        IndexBuffer,

        ConstantBuffer,
        ShaderResource,
        UnorderedAccess,

        RenderTarget,
        DepthStencilRead,
        DepthStencilWrite,

        Indirect,
        Predication,

        CopySource,
        CopyDestination,

        ResolveSource,
        ResolveDestination,

        RTAccelerationRead,
        RTAccelerationWrite,

        ShadingRateSource
    }

    public enum BarrierQueueSpec : byte
    {
        None = 0,

        Direct = 1 << 0,
        Compute = 1 << 1,
        Copy = 1 << 2,

        All = Direct | Compute | Copy
    }
}
