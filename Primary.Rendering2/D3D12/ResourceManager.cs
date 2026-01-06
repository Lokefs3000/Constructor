using Primary.Common;
using Primary.Profiling;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Resources;
using System.Diagnostics;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using System.Runtime.CompilerServices;

using D3D12MemAlloc = Interop.D3D12MemAlloc;

using static TerraFX.Interop.DirectX.D3D12;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.D3D12_CLEAR_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;

namespace Primary.Rendering2.D3D12
{
    [SupportedOSPlatform("windows")]
    internal sealed unsafe class ResourceManager
    {
        private readonly NRDDevice _device;

        private D3D12MemAlloc.Allocation* _resourcesMemory;
        private int _resourceMemorySize;

        private Dictionary<FrameGraphResource, Ptr<ID3D12Resource2>> _allocatedResources;
        private HashSet<FrameGraphResource> _initializedResources;

        private HashSet<FrameGraphResource> _pendingInitializes;

        internal ResourceManager(NRDDevice device)
        {
            _device = device;

            _resourcesMemory = null;
            _resourceMemorySize = 0;

            _allocatedResources = new Dictionary<FrameGraphResource, Ptr<ID3D12Resource2>>();
            _initializedResources = new HashSet<FrameGraphResource>();

            _pendingInitializes = new HashSet<FrameGraphResource>();

            Debug.Assert(D3D12MemAlloc.Allocator.IsTightAlignmentSupported(device.Allocator) != 0);
        }

        internal void PrepareForExecution(FrameGraphResources resources)
        {
            using (new ProfilingScope("Resources"))
            {
                FreeReferencedResources();

                Debug.Assert(_allocatedResources.Count == 0);

                if (_resourceMemorySize < resources.HighestMemoryUsage)
                {
                    if (_resourcesMemory != null)
                    {
                        _resourcesMemory->Base.Release();
                        _resourcesMemory = null;
                    }

                    D3D12MemAlloc.ALLOCATION_DESC allocDesc = new D3D12MemAlloc.ALLOCATION_DESC
                    {
                        Flags = D3D12MemAlloc.ALLOCATION_FLAGS.ALLOCATION_FLAG_CAN_ALIAS,
                        HeapType = D3D12_HEAP_TYPE_DEFAULT,
                        ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES
                    };

                    //D3D12_SMALL_RESOURCE_PLACEMENT_ALIGNMENT
                    D3D12_RESOURCE_ALLOCATION_INFO resAllocDesc = new D3D12_RESOURCE_ALLOCATION_INFO
                    {
                        SizeInBytes = (ulong)((long)resources.HighestMemoryUsage + (-(long)resources.HighestMemoryUsage & (256 - 1))),
                        Alignment = 256
                    };
                    
                    D3D12MemAlloc.Allocation* tempAllocPtr = null;

                    int r = D3D12MemAlloc.Allocator.AllocateMemory(_device.Allocator, &allocDesc, &resAllocDesc, &tempAllocPtr);
                    if (r != 0)
                    {
                        _device.RHIDevice.FlushMessageQueue();
                        throw new NotImplementedException("Add error message");
                    }

                    _resourcesMemory = tempAllocPtr;
                    _resourceMemorySize = resources.HighestMemoryUsage;
                }

                Guid* resourceGuid = UuidOf.Get<ID3D12Resource2>();
                foreach (ref readonly FGResourceLocation location in resources.Locations)
                {
                    D3D12_RESOURCE_DESC1 resDesc = default;
                    D3D12_CLEAR_VALUE clearValue = default;

                    D3D12_BARRIER_LAYOUT initialLayout = D3D12_BARRIER_LAYOUT_UNDEFINED;

                    switch (location.Resource.ResourceId)
                    {
                        case FGResourceId.Texture:
                            {
                                ref readonly FrameGraphTextureDesc texDesc = ref location.Resource.TextureDesc;

                                resDesc = new D3D12_RESOURCE_DESC1
                                {
                                    Dimension = FormatConverter.ToResourceDimension(texDesc.Dimension),
                                    Alignment = 0,
                                    Width = (ulong)texDesc.Width,
                                    Height = (uint)texDesc.Height,
                                    DepthOrArraySize = (ushort)texDesc.Depth,
                                    MipLevels = 1,
                                    Format = FormatConverter.ToDXGIFormat(texDesc.Format),
                                    SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                                    Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
                                    Flags = D3D12_RESOURCE_FLAG_USE_TIGHT_ALIGNMENT,
                                    SamplerFeedbackMipRegion = default
                                };

                                if (FlagUtility.HasFlag(texDesc.Usage, FGTextureUsage.RenderTarget))
                                {
                                    resDesc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;

                                    Color color = new Color(0.0f);
                                    clearValue = new D3D12_CLEAR_VALUE(resDesc.Format, (float*)&color);
                                }

                                if (FlagUtility.HasFlag(texDesc.Usage, FGTextureUsage.DepthStencil))
                                {
                                    resDesc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;

                                    clearValue = new D3D12_CLEAR_VALUE(resDesc.Format, 1.0f, 0xff);
                                }

                                break;
                            }
                        case FGResourceId.Buffer:
                            {
                                ref readonly FrameGraphBufferDesc bufDesc = ref location.Resource.BufferDesc;

                                resDesc = new D3D12_RESOURCE_DESC1
                                {
                                    Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
                                    Alignment = 0,
                                    Width = bufDesc.Width,
                                    Height = 1,
                                    DepthOrArraySize = 1,
                                    MipLevels = 1,
                                    Format = DXGI_FORMAT_UNKNOWN,
                                    SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                                    Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                                    Flags = D3D12_RESOURCE_FLAG_USE_TIGHT_ALIGNMENT,
                                    SamplerFeedbackMipRegion = default
                                };

                                break;
                            }
                    }

                    ID3D12Resource2* resourcePtr = null;
                    HRESULT r = D3D12MemAlloc.Allocator.CreateAliasingResource2(_device.Allocator, _resourcesMemory, (ulong)location.MemoryOffset, &resDesc, initialLayout, resDesc.Dimension == D3D12_RESOURCE_DIMENSION_BUFFER ? null : &clearValue, 0, null, resourceGuid, (void**)&resourcePtr);
             
                    if (r.FAILED)
                    {
                        _device.RHIDevice.ReviewError(r.Value);
                        _device.RHIDevice.FlushMessageQueue();
                        throw new NotImplementedException("Add error message");
                    }

                    if (location.Resource.DebugName != null)
                    {
                        ResourceUtility.SetResourceNameStack((ID3D12Resource*)resourcePtr, location.Resource.DebugName);
                    }

                    _allocatedResources[location.Resource] = resourcePtr;
                }
            }
        }

        internal void FreeReferencedResources()
        {
            foreach (var kvp in _allocatedResources)
            {
                kvp.Value.Pointer->Release(); //FIXME: GPU reference corruption
            }

            _allocatedResources.Clear();

            _initializedResources.Clear();
            _pendingInitializes.Clear();
        }

        internal ID3D12Resource2* GetResource(FrameGraphResource resource)
        {
            if (resource.IsExternal)
            {
                return resource.ResourceId switch
                {
                    FGResourceId.Texture => (ID3D12Resource2*)new RHI.Direct3D12.TextureInternal(Unsafe.As<RHI.Texture>(resource.Resource!)).Resource.NativePointer.ToPointer(),
                    FGResourceId.Buffer => (ID3D12Resource2*)new RHI.Direct3D12.BufferInternal(Unsafe.As<RHI.Buffer>(resource.Resource!)).Resource.NativePointer.ToPointer(),
                    _ => throw new NotImplementedException(),
                };
            }

            if (_allocatedResources.TryGetValue(resource, out Ptr<ID3D12Resource2> ptr))
                return ptr.Pointer;

            return null;
        }

        internal void EnsureInitialized(FrameGraphResource resource)
        {
            if (resource.IsExternal)
                return;
            if (_initializedResources.Contains(resource))
                return;

            _pendingInitializes.Add(resource);
        }

        internal void SetAsInitialized(FrameGraphResource resource)
        {
            if (resource.IsExternal)
                return;

            _initializedResources.Add(resource);
        }

        internal void FlushPendingInits(ID3D12GraphicsCommandList10* cmdList)
        {
            if (_pendingInitializes.Count == 0)
                return;

            foreach (FrameGraphResource resource in _pendingInitializes)
            {
                Debug.Assert(!_initializedResources.Contains(resource));
                Debug.Assert(!resource.IsExternal);

                if (resource.ResourceId == FGResourceId.Texture)
                {
                    ref readonly FrameGraphTextureDesc desc = ref resource.TextureDesc;

                    if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.RenderTarget))
                    {
                        _device.BarrierManager.AddTextureBarrier(resource.AsTexture(), D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
                    }
                    else if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.DepthStencil))
                    {
                        _device.BarrierManager.AddTextureBarrier(resource.AsTexture(), D3D12_BARRIER_SYNC_DEPTH_STENCIL, D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE, D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
                    }
                }
            }

            _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Texture);

            foreach (FrameGraphResource resource in _pendingInitializes)
            {
                if (resource.ResourceId == FGResourceId.Texture)
                {
                    ref readonly FrameGraphTextureDesc desc = ref resource.TextureDesc;

                    if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.RenderTarget))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource.AsTexture(), cmdList, D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
#endif

                        Color color = new Color(0.0f);
                        cmdList->ClearRenderTargetView(_device.RTVDescriptorHeap.GetDescriptorHandle(resource), (float*)&color, 0, null);
                    }
                    else if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.DepthStencil))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource.AsTexture(), cmdList, D3D12_BARRIER_SYNC_DEPTH_STENCIL, D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE, D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
#endif

                        cmdList->ClearDepthStencilView(_device.DSVDescriptorHeap.GetDescriptorHandle(resource), D3D12_CLEAR_FLAG_DEPTH | D3D12_CLEAR_FLAG_STENCIL, 1.0f, 0xff, 0, null);
                    }
                }

                _initializedResources.Add(resource);
            }

            _pendingInitializes.Clear();
        }

        internal static D3D12_RESOURCE_DESC1 GetResourceDescription(FrameGraphResource resource)
        {
            D3D12_RESOURCE_DESC1 resDesc = default;

            switch (resource.ResourceId)
            {
                case FGResourceId.Texture:
                    {
                        ref readonly FrameGraphTextureDesc texDesc = ref resource.TextureDesc;

                        resDesc = new D3D12_RESOURCE_DESC1
                        {
                            Dimension = FormatConverter.ToResourceDimension(texDesc.Dimension),
                            Alignment = 0,
                            Width = (ulong)texDesc.Width,
                            Height = (uint)texDesc.Height,
                            DepthOrArraySize = (ushort)texDesc.Depth,
                            MipLevels = 1,
                            Format = FormatConverter.ToDXGIFormat(texDesc.Format),
                            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                            Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
                            Flags = D3D12_RESOURCE_FLAG_USE_TIGHT_ALIGNMENT
                        };

                        if (FlagUtility.HasFlag(texDesc.Usage, FGTextureUsage.RenderTarget))
                        {
                            resDesc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
                        }

                        if (FlagUtility.HasFlag(texDesc.Usage, FGTextureUsage.DepthStencil))
                        {
                            resDesc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
                        }

                        break;
                    }
                case FGResourceId.Buffer:
                    {
                        ref readonly FrameGraphBufferDesc bufDesc = ref resource.BufferDesc;

                        resDesc = new D3D12_RESOURCE_DESC1
                        {
                            Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
                            Alignment = 0,
                            Width = bufDesc.Width,
                            Height = 1,
                            DepthOrArraySize = 1,
                            MipLevels = 1,
                            Format = DXGI_FORMAT_UNKNOWN,
                            SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                            Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                            Flags = D3D12_RESOURCE_FLAG_USE_TIGHT_ALIGNMENT
                        };

                        break;
                    }
            }

            return resDesc;
        }

        internal static D3D12_RESOURCE_DESC1 GetBufferDescription(int width)
        {
            D3D12_RESOURCE_DESC1 resDesc  = new D3D12_RESOURCE_DESC1
            {
                Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
                Alignment = 0,
                Width = (ulong)width,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = DXGI_FORMAT_UNKNOWN,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
                Flags = D3D12_RESOURCE_FLAG_USE_TIGHT_ALIGNMENT
            };

            return resDesc;
        }
    }
}
