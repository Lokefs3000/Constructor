using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Primary.Utility;
using SharpGen.Runtime;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Primary.Rendering.D3D12
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

            _descriptorHandleSize = (int)device.Device->GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);
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
                    heap.Heap.Dispose();
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

            CpuDescriptorHandle dstDescriptor = new CpuDescriptorHandle((nuint)(heap.StartHandle.Ptr + (ulong)_activeHeapOffset));

            SamplerDesc2 desc = new SamplerDesc2
            {
                Filter = sampler.Description.MaxAnisotropy > 1 ?
                    ResourceHelper.EncodeAnisotropicFilter(sampler.Description.Reduction) :
                    ResourceHelper.EncodeBasicFilter(sampler.Description.Min, sampler.Description.Mag, sampler.Description.Mip, sampler.Description.Reduction),
                AddressU = sampler.Description.AddressModeU.ToTextureAddressMode(),
                AddressV = sampler.Description.AddressModeV.ToTextureAddressMode(),
                AddressW = sampler.Description.AddressModeW.ToTextureAddressMode(),
                MipLODBias = sampler.Description.MipLODBias,
                MaxAnisotropy = sampler.Description.MaxAnisotropy,
                ComparisonFunc = sampler.Description.ComparisonFunction.ToComparisonFunc(),
                MinLOD = sampler.Description.MinLOD,
                MaxLOD = sampler.Description.MaxLOD,
                Flags = SamplerFlags.None
            };

            if (sampler.Description.BorderColorAsUInt)
            {
                desc.Flags |= SamplerFlags.UintBorderColor;
                Unsafe.WriteUnaligned(ref Unsafe.AsRef<byte>(desc.Anonymous.UintBorderColor), sampler.Description.BorderColor);
            }
            else
                Unsafe.WriteUnaligned(ref Unsafe.AsRef<byte>(desc.Anonymous.FloatBorderColor), sampler.Description.BorderColor);

            _device.Device->CreateSampler2(&desc, dstDescriptor);

            ++_descriptorsUsedThisFrame;
            _activeHeapOffset += _descriptorHandleSize;

            return index;
        }

        private void AddNewHeapToList()
        {
            DescriptorHeapDesc desc = new DescriptorHeapDesc
            {
                Type = DescriptorHeapType.Sampler,
                NumDescriptors = (uint)_descriptorHeapSize,
                Flags = DescriptorHeapFlags.ShaderVisible,
                NodeMask = 0
            };

            ComPtr<ID3D12DescriptorHeap> descriptorHeap = new ComPtr<ID3D12DescriptorHeap>();
            HResult hr = _device.Device->CreateDescriptorHeap(&desc, out descriptorHeap);

            if (hr.IsFailure)
            {
                _device.RHIDevice.FlushPendingMessages();
                throw new NotImplementedException("Add error message");
            }

            _activeHeaps.Add(new HeapData(descriptorHeap, descriptorHeap.GetCPUDescriptorHandleForHeapStart()));
        }

        internal ID3D12DescriptorHeap* GetActiveHeapOrCreateNew()
        {
            if (_activeHeapIndex < _activeHeaps.Count)
                return (ID3D12DescriptorHeap*)Unsafe.AsPointer(ref _activeHeaps[_activeHeapIndex].Heap.Get());

            AddNewHeapToList();
            return (ID3D12DescriptorHeap*)Unsafe.AsPointer(ref _activeHeaps[_activeHeapIndex].Heap.Get());
        }

        internal ref ID3D12DescriptorHeap CurrentActiveHeap
        {
            get
            {
                if (_activeHeapIndex < _activeHeaps.Count)
                    return ref _activeHeaps[_activeHeapIndex].Heap.Get();
                else
                    return ref Unsafe.NullRef<ID3D12DescriptorHeap>();
            }
        }

        internal readonly record struct HeapData(ComPtr<ID3D12DescriptorHeap> Heap, CpuDescriptorHandle StartHandle);
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
