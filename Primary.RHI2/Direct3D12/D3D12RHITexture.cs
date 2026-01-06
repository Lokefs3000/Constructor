using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.DirectX;

using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_SRV_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_BUFFER_SRV_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_BUFFER_UAV_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static Interop.D3D12MemAlloc.ALLOCATION_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;

using D3D12MA = Interop.D3D12MemAlloc;
using TerraFX.Interop.Windows;
using System.Runtime.Versioning;
using Primary.Common;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Primary.RHI2.Direct3D12
{
    [SupportedOSPlatform("windows")]
    public unsafe sealed class D3D12RHITexture : RHITexture
    {
        private readonly D3D12RHIDevice _device;

        private ComPtr<ID3D12Resource2> _resource;
        private D3D12MA.Allocation* _allocation;

        private D3D12_BARRIER_SYNC _barrierSync;
        private D3D12_BARRIER_ACCESS _barrierAccess;
        private D3D12_BARRIER_LAYOUT _barrierLayout;

        private D3D12RHITextureNative* _nativeRep;

        internal D3D12RHITexture(D3D12RHIDevice device, RHITextureDescription description)
        {
            _device = device;
            _description = description;

            {
                D3D12_RESOURCE_DESC1 desc = new D3D12_RESOURCE_DESC1
                {
                    Dimension = description.Dimension.ToResourceDimension(),
                    Alignment = 0,
                    Width = (ulong)description.Width,
                    Height = (uint)description.Height,
                    DepthOrArraySize = (ushort)description.DepthOrArraySize,
                    MipLevels = (ushort)description.MipLevels,
                    Format = description.Format.ToTextureFormat(),
                    SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                    Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
                    Flags = D3D12_RESOURCE_FLAG_USE_TIGHT_ALIGNMENT,
                    SamplerFeedbackMipRegion = default
                };

                if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.UnorderedAccess))
                    desc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;

                D3D12MA.ALLOCATION_DESC alloc = new D3D12MA.ALLOCATION_DESC
                {
                    Flags = ALLOCATION_FLAG_NONE,
                    HeapType = D3D12_HEAP_TYPE_DEFAULT,
                    ExtraHeapFlags = D3D12_HEAP_FLAG_NONE,
                    CustomPool = null,
                    pPrivateData = null,
                };

                D3D12MA.Allocation* temp = null;
                HRESULT hr = D3D12MA.Allocator.CreateResource3(device.Allocator, &alloc, &desc, D3D12_BARRIER_LAYOUT_UNDEFINED, null, 0, null, &temp, UuidOf.Get<ID3D12Resource2>(), (void**)_resource.GetAddressOf());
                if (hr.FAILED)
                {
                    throw new RHIException($"Failed to create D3D12 resource: {hr}");
                }

                _allocation = temp;
            }

            _barrierSync = D3D12_BARRIER_SYNC_NONE;
            _barrierAccess = D3D12_BARRIER_ACCESS_COMMON;
            _barrierLayout = D3D12_BARRIER_LAYOUT_UNDEFINED;

            {
                _nativeRep = (D3D12RHITextureNative*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<D3D12RHITextureNative>());
                _nativeRep->Base = new RHITextureNative
                {
                    Description = description,
                };
                _nativeRep->Resource = _resource;
                _nativeRep->Memory = _allocation;
                _nativeRep->BarrierSync = (D3D12_BARRIER_SYNC*)Unsafe.AsPointer(ref _barrierSync);
                _nativeRep->BarrierAccess = (D3D12_BARRIER_ACCESS*)Unsafe.AsPointer(ref _barrierAccess);
                _nativeRep->BarrierLayout = (D3D12_BARRIER_LAYOUT*)Unsafe.AsPointer(ref _barrierLayout);
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

                    if (_allocation != null)
                        _allocation->Base.Dispose();
                    _allocation = null;
                    _resource.Reset();
                });

                _disposedValue = true;
            }
        }

        protected override void SetDebugName(string? debugName)
        {
            if (_resource.Get() != null)
            {
                ResourceHelper.SetResourceName(_resource, debugName);
            }
        }

        public override unsafe RHITextureNative* GetAsNative() => (RHITextureNative*)_nativeRep;

        public override RHIResourceType Type => RHIResourceType.Texture;
    }

    public unsafe struct D3D12RHITextureNative
    {
        public RHITextureNative Base;

        public ID3D12Resource2* Resource;
        public D3D12MA.Allocation* Memory;

        public D3D12_BARRIER_SYNC* BarrierSync;
        public D3D12_BARRIER_ACCESS* BarrierAccess;
        public D3D12_BARRIER_LAYOUT* BarrierLayout;

        public static implicit operator RHITextureNative(D3D12RHITextureNative native) => native.Base;
    }
}
