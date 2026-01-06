using CommunityToolkit.HighPerformance;
using Primary.Rendering2.Resources;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Versioning;
using System.Text;
using TerraFX.Interop.DirectX;

using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_SAMPLER_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_DESCRIPTOR_HEAP_FLAGS;
using Primary.Common;
using TerraFX.Interop.Windows;
using Primary.RHI2;

namespace Primary.Rendering2.D3D12
{
    [SupportedOSPlatform("windows")]
    internal unsafe sealed class SamplerDescriptorHeap
    {
        private readonly NRDDevice _device;

        private readonly int _descriptorHandleSize;
        private readonly int _descriptorHeapSize;

        private AverageAnalyser<int> _averageDescriptorUse;
        private int _descriptorsUsedThisFrame;

        private List<HeapData> _activeHeaps;
        private int _activeHeapIndex;
        private int _activeHeapOffset;

        private Dictionary<SamplerDesc, uint> _activeDescriptors;

        private bool _disposedValue;

        internal SamplerDescriptorHeap(NRDDevice device, int heapSize)
        {
            _device = device;

            _descriptorHandleSize = (int)device.Device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER);
            _descriptorHeapSize = heapSize;

            _averageDescriptorUse = new AverageAnalyser<int>(16, 0.0f);
            _descriptorsUsedThisFrame = 0;

            _activeHeaps = new List<HeapData>();
            _activeHeapIndex = 0;
            _activeHeapOffset = 0;

            _activeDescriptors = new Dictionary<SamplerDesc, uint>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                foreach (HeapData heap in _activeHeaps)
                    heap.Heap.Pointer->Release();
                _activeHeaps.Clear();

                _disposedValue = true;
            }
        }

        ~SamplerDescriptorHeap()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ResetForNewFrame()
        {
            _averageDescriptorUse.Sample(_descriptorsUsedThisFrame, 1.0f);
            _descriptorsUsedThisFrame = 0;

            _activeHeapIndex = 0;
            _activeHeapOffset = 0;

            _activeDescriptors.Clear();
        }

        internal uint GetDescriptorIndex(SamplerDesc sampler, out bool changedActiveHeap)
        {
            changedActiveHeap = false;

            if (_activeDescriptors.TryGetValue(sampler, out uint index))
                return index;

            if (_activeHeapIndex >= _activeHeaps.Count || _activeHeapOffset >= _descriptorHeapSize)
            {
                if (_activeHeapIndex >= _activeHeaps.Count)
                    AddNewHeapToList();
                else
                    _activeHeapIndex++;

                _activeDescriptors.Clear();
                _activeHeapOffset = 0;

                changedActiveHeap = true;
            }

            HeapData heap = _activeHeaps[_activeHeapIndex];

            index = (uint)_activeDescriptors.Count;

            _activeDescriptors[sampler] = index;

            D3D12_CPU_DESCRIPTOR_HANDLE dstDescriptor = new D3D12_CPU_DESCRIPTOR_HANDLE(heap.StartHandle, _activeHeapOffset);

            D3D12_SAMPLER_DESC2 desc = new D3D12_SAMPLER_DESC2
            {
                Filter = FormatConverter.ToFilter(sampler.Description.Filter),
                AddressU = FormatConverter.ToAddressMode(sampler.Description.AddressModeU),
                AddressV = FormatConverter.ToAddressMode(sampler.Description.AddressModeV),
                AddressW = FormatConverter.ToAddressMode(sampler.Description.AddressModeW),
                MipLODBias = sampler.Description.MipLODBias,
                MaxAnisotropy = sampler.Description.MaxAnisotropy,
                ComparisonFunc = FormatConverter.ToComparisonFunc(sampler.Description.ComparisonFunc),
                MinLOD = sampler.Description.MinLOD,
                MaxLOD = sampler.Description.MaxLOD,
                Flags = D3D12_SAMPLER_FLAG_NONE
            };

            if (sampler.Description.BorderColor.HasValue)
                Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref desc.FloatBorderColor.DangerousGetReference()), sampler.Description.BorderColor);

            _device.Device->CreateSampler2(&desc, dstDescriptor);

            ++_descriptorsUsedThisFrame;
            _activeHeapOffset += _descriptorHandleSize;

            return index;
        }

        private void AddNewHeapToList()
        {
            D3D12_DESCRIPTOR_HEAP_DESC desc = new D3D12_DESCRIPTOR_HEAP_DESC
            {
                Type = D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER,
                NumDescriptors = (uint)_descriptorHeapSize,
                Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE,
                NodeMask = 0
            };

            ID3D12DescriptorHeap* temp = null;
            HRESULT hr = _device.Device->CreateDescriptorHeap(&desc, UuidOf.Get<ID3D12DescriptorHeap>(), (void**)&temp);

            if (hr.FAILED)
            {
                _device.RHIDevice.FlushMessageQueue();
                throw new NotImplementedException("Add error message");
            }

            _activeHeaps.Add(new HeapData(temp, temp->GetCPUDescriptorHandleForHeapStart()));
        }

        internal ID3D12DescriptorHeap* GetActiveHeapOrCreateNew()
        {
            if (_activeHeapIndex < _activeHeaps.Count)
                return _activeHeaps[_activeHeapIndex].Heap.Pointer;

            AddNewHeapToList();
            return _activeHeaps[_activeHeapIndex].Heap.Pointer;
        }

        internal ID3D12DescriptorHeap* CurrentActiveHeap => _activeHeapIndex < _activeHeaps.Count ? _activeHeaps[_activeHeapIndex].Heap.Pointer : null;
        
        internal readonly record struct HeapData(Ptr<ID3D12DescriptorHeap> Heap, D3D12_CPU_DESCRIPTOR_HANDLE StartHandle);
    }

    internal readonly record struct SamplerDesc(RHISamplerDescription Description) : IEquatable<SamplerDesc>
    {
        public override int GetHashCode()
        {
            return MemoryMarshal.Cast<SamplerDesc, byte>(new ReadOnlySpan<SamplerDesc>(in this)).GetDjb2HashCode();
        }

        public static implicit operator SamplerDesc(RHISamplerDescription desc) => new SamplerDesc(desc);
    }
}
