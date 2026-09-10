using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Primary.Timing;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using static Interop.D3D12MemAlloc.ALLOCATION_FLAGS;
using D3D12MA = Interop.D3D12MemAlloc;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe sealed class D3D12RHIReadback : RHIReadback
    {
        private readonly D3D12RHIDevice _device;

        private ComPtr<ID3D12Resource2> _resource;
        private D3D12MA.Allocation* _allocation;

        private D3D12RHIReadbackNative* _nativeRep;

        internal D3D12RHIReadback(D3D12RHIDevice device, RHIReadbackDescription description)
        {
            _device = device;
            _description = description;

            {
                ResourceDesc1 desc = new ResourceDesc1
                {
                    Dimension = ResourceDimension.Buffer,
                    Alignment = 0,
                    Width = description.Width,
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Format = Format.FormatUnknown,
                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                    Layout = TextureLayout.LayoutRowMajor,
                    Flags = device.Setup.UseTightAlignment ? ResourceFlags.UseTightAlignment : ResourceFlags.None,
                    SamplerFeedbackMipRegion = default
                };

                D3D12MA.ALLOCATION_DESC alloc = new D3D12MA.ALLOCATION_DESC
                {
                    Flags = ALLOCATION_FLAG_NONE,
                    HeapType = HeapType.Readback,
                    ExtraHeapFlags = HeapFlags.None,
                    CustomPool = null,
                    pPrivateData = null,
                };

                D3D12MA.Allocation* temp = null;
                HResult hr = D3D12MA.Allocator.CreateResource3(device.Allocator, &alloc, &desc, BarrierLayout.Undefined, null, 0, null, &temp, SilkMarshal.GuidPtrOf<ID3D12Resource2>(), (void**)_resource.GetAddressOf());
                if (hr.IsFailure)
                {
                    throw new D3D12RHIException($"Failed to create D3D12 resource", hr.Value);
                }

                _allocation = temp;
            }

            {
                _nativeRep = (D3D12RHIReadbackNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIReadbackNative>());
                _nativeRep->Base = new RHIReadbackNative
                {
                    Description = description,
                };
                _nativeRep->Resource = _resource;
                _nativeRep->Memory = _allocation;
                _nativeRep->BarrierSync = BarrierSync.All;
                _nativeRep->BarrierAccess = BarrierAccess.Common;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _device.AddResourceFreeNextFrame(() =>
                {
                    if (_nativeRep != null)
                        NativeMemory.Free(_nativeRep);
                    _nativeRep = null;

                    _resource.Dispose();
                    if (_allocation != null)
                        _allocation->Base.Release();
                    _allocation = null;

                    _device.ResourceTracker.Untrack(this);
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {
            if (!Unsafe.IsNullRef(in _resource.Get()))
            {
                ResourceHelper.SetResourceName(ref _resource.Get(), debugName);
            }
        }

        public override string ToString()
        {
            return $"RHIReadback{{{_debugName}}}";
        }

        public override bool Read(Span<byte> data)
        {
            if (!_nativeRep->HasDataReady)
                return false;

            if (!data.IsEmpty)
            {
                Silk.NET.Direct3D12.Range readRange = new Silk.NET.Direct3D12.Range(0, (nuint)Math.Min(_description.Width, data.Length));
                void* mapPtr = null;

                if (new HResult(_resource.Map(0, ref readRange, ref mapPtr)).IsFailure)
                    return false;

                try
                {
                    new Span<byte>(mapPtr, (int)readRange.End).CopyTo(data);
                }
                finally
                {
                    Silk.NET.Direct3D12.Range emptyRange = new Silk.NET.Direct3D12.Range(0, 0);
                    _resource.Unmap(0, ref emptyRange);
                }
            }

            return true;
        }

        public override bool Read<T>(Span<T> data)
        {
            return Read(MemoryMarshal.Cast<T, byte>(data));
        }

        public override bool IsDataReady => _nativeRep->HasDataReady;

        public override unsafe RHIReadbackNative* GetAsNative() => (RHIReadbackNative*)_nativeRep;

        public ComPtr<ID3D12Resource2> Resource => _resource;
    }

    public unsafe struct D3D12RHIReadbackNative
    {
        public RHIReadbackNative Base;

        public ID3D12Resource2* Resource;
        public D3D12MA.Allocation* Memory;

        public BarrierSync BarrierSync;
        public BarrierAccess BarrierAccess;

        public bool HasDataReady;

        public static implicit operator RHIReadbackNative(D3D12RHIReadbackNative native) => native.Base;
    }
}
