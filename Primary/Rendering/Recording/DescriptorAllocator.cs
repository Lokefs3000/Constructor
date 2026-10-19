using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Rendering.Commands;
using Primary.RHI;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Recording
{
    internal sealed class DescriptorAllocator
    {
        private readonly Dictionary<DescriptorKey, DescriptorData> _descriptors;
        private readonly List<DescriptorAllocationInfo> _allocationInfos;

        private readonly Dictionary<RHISampler, DescriptorData> _samplerDescriptors;
        private readonly List<SamplerDescriptorAllocationInfo> _samplerAllocationInfos;

        private TransientHeapData _renderTargetHeap;
        private TransientHeapData _depthStencilHeap;
        private TransientHeapData _cpuResourceViewHeap;
        private TransientHeapData _gpuResourceViewHeap;
        private TransientHeapData _samplerHeap;

        internal DescriptorAllocator(int rtHeapMaxSize, int dsHeapMaxSize, int rvCpuMaxHeapSize, int rvGpuMaxHeapSize, int samplerMaxHeapSize)
        {
            _descriptors = new Dictionary<DescriptorKey, DescriptorData>();
            _allocationInfos = new List<DescriptorAllocationInfo>();

            _samplerDescriptors = new Dictionary<RHISampler, DescriptorData>();
            _samplerAllocationInfos = new List<SamplerDescriptorAllocationInfo>();

            _renderTargetHeap = new TransientHeapData(0, 0, rtHeapMaxSize);
            _depthStencilHeap = new TransientHeapData(0, 0, dsHeapMaxSize);
            _cpuResourceViewHeap = new TransientHeapData(0, 0, rvCpuMaxHeapSize);
            _gpuResourceViewHeap = new TransientHeapData(0, 0, rvGpuMaxHeapSize);
            _samplerHeap = new TransientHeapData(0, 0, samplerMaxHeapSize);
        }

        internal void ClearDescriptorData()
        {
            _descriptors.Clear();
            _allocationInfos.Clear();

            _samplerDescriptors.Clear();
            _samplerAllocationInfos.Clear();

            _renderTargetHeap.Reset();
            _depthStencilHeap.Reset();
            _cpuResourceViewHeap.Reset();
            _gpuResourceViewHeap.Reset();
            _samplerHeap.Reset();
        }

        internal DescriptorData GetDescriptorForRenderTarget(ResourceHandle resource)
        {
            ref DescriptorData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_descriptors, new DescriptorKey(DescriptorHeapType.RenderTarget, resource), out bool exists);
            if (!exists)
            {
                (int heap, int index) = _renderTargetHeap.Allocate(out _);
                data = new DescriptorData(-1, -1, -1, heap, _allocationInfos.Count, index);

                _allocationInfos.Add(new DescriptorAllocationInfo(DescriptorHeapType.RenderTarget, resource, false, heap, index));
            }

            return data;
        }

        internal DescriptorData GetDescriptorForDepthStencil(ResourceHandle resource)
        {
            ref DescriptorData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_descriptors, new DescriptorKey(DescriptorHeapType.DepthStencil, resource), out bool exists);
            if (!exists)
            {
                (int heap, int index) = _depthStencilHeap.Allocate(out _);
                data = new DescriptorData(-1, -1, -1, heap, _allocationInfos.Count, index);

                _allocationInfos.Add(new DescriptorAllocationInfo(DescriptorHeapType.DepthStencil, resource, false, heap, index));
            }

            return data;
        }

        internal DescriptorData GetDescriptorForResourceViewCPU(DescriptorHeapType heapType, ResourceHandle resource)
        {
            ref DescriptorData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_descriptors, new DescriptorKey(heapType, resource), out bool exists);
            if (!exists)
            {
                (int heap, int index) = _cpuResourceViewHeap.Allocate(out _);
                data = new DescriptorData(-1, -1, -1, heap, _allocationInfos.Count, index);

                _allocationInfos.Add(new DescriptorAllocationInfo(heapType, resource, false, heap, index));
            }
            else if (!data.HasCPUDescriptor)
            {
                (int heap, int index) = _cpuResourceViewHeap.Allocate(out _);
                data = new DescriptorData(data.GPUHeapIndex, data.GPUDescriptorIndex, data.GPULocalIndex, heap, _allocationInfos.Count, index);

                _allocationInfos.Add(new DescriptorAllocationInfo(heapType, resource, false, heap, index));
            }

            return data;
        }

        internal DescriptorData GetDescriptorForResourceViewGPU(DescriptorHeapType heapType, ResourceHandle resource, out bool updateDescriptorHeap)
        {
            ref DescriptorData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_descriptors, new DescriptorKey(heapType, resource), out bool exists);
            if (!exists)
            {
                (int heap, int index) = _gpuResourceViewHeap.Allocate(out updateDescriptorHeap);
                data = new DescriptorData(heap, _allocationInfos.Count, index, -1, -1, -1);

                _allocationInfos.Add(new DescriptorAllocationInfo(heapType, resource, true, heap, index));
            }
            else if (!data.HasGPUDescriptor)
            {
                (int heap, int index) = _gpuResourceViewHeap.Allocate(out updateDescriptorHeap);
                data = new DescriptorData(heap, _allocationInfos.Count, index, data.CPUHeapIndex, data.CPUDescriptorIndex, data.CPULocalIndex);

                _allocationInfos.Add(new DescriptorAllocationInfo(heapType, resource, true, heap, index));
            }
            else
            {
                updateDescriptorHeap = false;
            }

            return data;
        }

        internal DescriptorData GetDescriptorForResourceViewCPUGPU(DescriptorHeapType heapType, ResourceHandle resource, out bool updateDescriptorHeap)
        {
            ref DescriptorData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_descriptors, new DescriptorKey(heapType, resource), out bool exists);
            if (!exists)
            {
                (int gpuHeap, int gpuIndex) = _gpuResourceViewHeap.Allocate(out updateDescriptorHeap);
                (int cpuHeap, int cpuIndex) = _cpuResourceViewHeap.Allocate(out _);
                data = new DescriptorData(gpuHeap, _allocationInfos.Count + 1, gpuIndex, cpuHeap, _allocationInfos.Count, cpuIndex);

                _allocationInfos.Add(new DescriptorAllocationInfo(heapType, resource, false, cpuHeap, cpuIndex));
                _allocationInfos.Add(new DescriptorAllocationInfo(heapType, resource, true, gpuHeap, gpuIndex));
            }
            else if (!data.HasCPUDescriptor)
            {
                (int heap, int index) = _cpuResourceViewHeap.Allocate(out _);
                data = new DescriptorData(data.GPUHeapIndex, data.GPUDescriptorIndex, data.GPULocalIndex, heap, _allocationInfos.Count, index);
                updateDescriptorHeap = false;

                _allocationInfos.Add(new DescriptorAllocationInfo(heapType, resource, false, heap, index));
            }
            else if (!data.HasGPUDescriptor)
            {
                (int heap, int index) = _gpuResourceViewHeap.Allocate(out updateDescriptorHeap);
                data = new DescriptorData(heap, _allocationInfos.Count, index, data.CPUHeapIndex, data.CPUDescriptorIndex, data.CPULocalIndex);

                _allocationInfos.Add(new DescriptorAllocationInfo(heapType, resource, true, heap, index));
            }
            else
            {
                updateDescriptorHeap = false;
            }

            return data;
        }

        internal DescriptorData GetDescriptorForSampler(RHISampler sampler)
        {
            ref DescriptorData data = ref CollectionsMarshal.GetValueRefOrAddDefault(_samplerDescriptors, sampler, out bool exists);
            if (!exists)
            {
                (int heap, int index) = _samplerHeap.Allocate(out _);
                data = new DescriptorData(heap, _samplerAllocationInfos.Count, index, -1, -1, -1);

                _samplerAllocationInfos.Add(new SamplerDescriptorAllocationInfo(sampler, heap, index));
            }

            return data;
        }

        internal void RecordDescriptorSets(CommandRecorder recorder)
        {
            recorder.RecordCommand(CmdCommandType.SetDescriptorHeap, new CmdSetDescriptorHeap
            {
                GPUHeapIndex = _gpuResourceViewHeap.HeapIndex,
                SamplerHeapIndex = _samplerHeap.HeapIndex
            });

            _descriptors.Clear();
        }

        internal ref readonly TransientHeapData RenderTargetHeap => ref _renderTargetHeap;
        internal ref readonly TransientHeapData DepthStencilHeap => ref _depthStencilHeap;
        internal ref readonly TransientHeapData CPUResourceViewHeap => ref _cpuResourceViewHeap;
        internal ref readonly TransientHeapData GPUResourceViewHeap => ref _gpuResourceViewHeap;
        internal ref readonly TransientHeapData SamplerHeap => ref _samplerHeap;

        internal ROList<DescriptorAllocationInfo> ResourceDescriptors => _allocationInfos;
        internal ROList<SamplerDescriptorAllocationInfo> SamplerDescriptors => _samplerAllocationInfos;

        private readonly record struct DescriptorKey(DescriptorHeapType HeapType, ResourceHandle Handle) : IEquatable<DescriptorKey>
        {
            public override int GetHashCode() => HashCode.Combine(HeapType, Handle);
        }
    }

    internal record struct TransientHeapData(int HeapIndex, int DescriptorIndex, int HeapSizeLimit)
    {
        public void Reset()
        {
            HeapIndex = 0;
            DescriptorIndex = 0;
        }

        public (int Heap, int Index) Allocate(out bool heapIncremented)
        {
            heapIncremented = false;

            int heap = HeapIndex;
            int index = DescriptorIndex++;

            if (DescriptorIndex >= HeapSizeLimit)
            {
                ++HeapIndex;
                DescriptorIndex = 0;

                heapIncremented = true;
            }

            return (heap, heap * HeapSizeLimit + index);
        }
    }

    internal readonly record struct DescriptorAllocationInfo(DescriptorHeapType HeapType, ResourceHandle Resource, bool IsGPUHeap, int HeapIndex, int DescriptorIndex);
    internal readonly record struct SamplerDescriptorAllocationInfo(RHISampler Sampler, int HeapIndex, int DescriptorIndex);
    internal readonly record struct DescriptorData(int GPUHeapIndex, int GPUDescriptorIndex, int GPULocalIndex, int CPUHeapIndex, int CPUDescriptorIndex, int CPULocalIndex)
    {
        public bool HasGPUDescriptor => GPUDescriptorIndex != -1;
        public bool HasCPUDescriptor => CPUDescriptorIndex != -1;
    }

    internal enum DescriptorHeapType : byte
    {
        RenderTarget = 0,
        DepthStencil,
        ConstantBufferView,
        ShaderResourceView,
        UnorderedAccessView
    }
}
