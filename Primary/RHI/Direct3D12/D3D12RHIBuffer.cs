using Primary.Common;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static Interop.D3D12MemAlloc.ALLOCATION_FLAGS;
using D3D12MA = Interop.D3D12MemAlloc;

namespace Primary.RHI.Direct3D12
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public unsafe sealed class D3D12RHIBuffer : RHIBuffer
    {
        private readonly D3D12RHIDevice _device;

        private ComPtr<ID3D12Resource2> _resource;
        private D3D12MA.Allocation* _allocation;

        private BarrierSync _barrierSync;
        private BarrierAccess _barrierAccess;

        private D3D12RHIBufferNative* _nativeRep;

        internal D3D12RHIBuffer(D3D12RHIDevice device, RHIBufferDescription description)
        {
            if (Flags.HasFlag(description.Usage, RHIResourceUsage.ConstantBuffer) && description.Width < 256)
            {
                description.Width = 256;
            }

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

                if (Flags.HasFlag(description.Usage, RHIResourceUsage.UnorderedAccess))
                    desc.Flags |= ResourceFlags.AllowUnorderedAccess;

                D3D12MA.ALLOCATION_DESC alloc = new D3D12MA.ALLOCATION_DESC
                {
                    Flags = ALLOCATION_FLAG_NONE,
                    HeapType = HeapType.Default,
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

            _barrierSync = BarrierSync.All;
            _barrierAccess = BarrierAccess.Common;

            {
                _nativeRep = (D3D12RHIBufferNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHIBufferNative>());
                _nativeRep->Base = new RHIBufferNative
                {
                    Description = description,
                };
                _nativeRep->Resource = _resource;
                _nativeRep->Memory = _allocation;
                _nativeRep->BarrierSync = _barrierSync;
                _nativeRep->BarrierAccess = _barrierAccess;
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

                    _device.UploadManager.RemoveWithResource(this);
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
            return $"RHIBuffer{{{_debugName}}}";
        }

        public override unsafe RHIBufferNative* GetAsNative() => (RHIBufferNative*)_nativeRep;
        public override unsafe RHIResourceNative* GetBaseAsNative() => (RHIResourceNative*)_nativeRep;

        public override RHIResourceType Type => RHIResourceType.Buffer;

        public ComPtr<ID3D12Resource2> Resource => _resource;
    }

    public unsafe struct D3D12RHIBufferNative
    {
        public RHIBufferNative Base;

        public ID3D12Resource2* Resource;
        public D3D12MA.Allocation* Memory;

        public BarrierSync BarrierSync;
        public BarrierAccess BarrierAccess;

        public static implicit operator RHIBufferNative(D3D12RHIBufferNative native) => native.Base;
    }
}
