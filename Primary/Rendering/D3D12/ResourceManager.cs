using Primary.Common;
using Primary.Profiling;
using Primary.Rendering.Pass;
using Primary.Rendering.Resources;
using Primary.RHI2;
using Primary.RHI2.Direct3D12;
using System.Diagnostics;
using System.Runtime.Versioning;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_ACCESS;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_LAYOUT;
using static TerraFX.Interop.DirectX.D3D12_BARRIER_SYNC;
using static TerraFX.Interop.DirectX.D3D12_CLEAR_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_HEAP_TYPE;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DirectX.D3D12_RESOURCE_FLAGS;
using static TerraFX.Interop.DirectX.D3D12_TEXTURE_LAYOUT;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.D3D12;
using D3D12MemAlloc = Interop.D3D12MemAlloc;
using CommunityToolkit.HighPerformance;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal sealed unsafe class ResourceManager : IDisposable
    {
        private readonly NRDDevice _device;

        private D3D12MemAlloc.Allocation* _resourcesMemory;
        private int _resourceMemorySize;

        private Dictionary<NRDResource, Ptr<ID3D12Resource2>> _allocatedResources;
        private HashSet<NRDResource> _initializedResources;

        private HashSet<NRDResource> _pendingInitializes;

        private FrameGraphResources? _frameResourceData;
        private int _currentEventIndex;

        private bool _disposedValue;

        internal ResourceManager(NRDDevice device)
        {
            _device = device;

            _resourcesMemory = null;
            _resourceMemorySize = 0;

            _allocatedResources = new Dictionary<NRDResource, Ptr<ID3D12Resource2>>();
            _initializedResources = new HashSet<NRDResource>();

            _pendingInitializes = new HashSet<NRDResource>();

            _frameResourceData = null;
            _currentEventIndex = 0;

            Debug.Assert(D3D12MemAlloc.Allocator.IsTightAlignmentSupported(device.Allocator) != 0);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                FreeReferencedResources();

                if (_resourcesMemory != null)
                    _resourcesMemory->Base.Release();
                _resourcesMemory = null;

                _disposedValue = true;
            }
        }

        ~ResourceManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void PrepareForExecution(FrameGraphResources resources)
        {
            using (new ProfilingScope("Resources"))
            {
                FreeReferencedResources();

                _frameResourceData = resources;
                _currentEventIndex = 0;

                Debug.Assert(_allocatedResources.Count == 0);

                const int SafetyNetSize = D3D12_DEFAULT_RESOURCE_PLACEMENT_ALIGNMENT;

                int memoryUsageRequired = resources.HighestMemoryUsage + SafetyNetSize;
                memoryUsageRequired = (memoryUsageRequired + (-memoryUsageRequired & 255));

                if (_resourceMemorySize < memoryUsageRequired)
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
                        ExtraHeapFlags = D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES,
                    };

                    D3D12_RESOURCE_ALLOCATION_INFO resAllocDesc = new D3D12_RESOURCE_ALLOCATION_INFO
                    {
                        SizeInBytes = (ulong)memoryUsageRequired,
                        Alignment = 256
                    };

                    D3D12MemAlloc.Allocation* tempAllocPtr = null;

                    int r = D3D12MemAlloc.Allocator.AllocateMemory(_device.Allocator, &allocDesc, &resAllocDesc, &tempAllocPtr);
                    if (r != 0)
                    {
                        _device.RHIDevice.FlushPendingMessages();
                        throw new NotImplementedException("Add error message");
                    }

                    _resourcesMemory = tempAllocPtr;
                    _resourceMemorySize = memoryUsageRequired;
                }

                Guid* resourceGuid = UuidOf.Get<ID3D12Resource2>();
                foreach (ref readonly FGResourceLocation location in resources.Locations)
                {
                    D3D12_RESOURCE_DESC1 resDesc = default;
                    D3D12_CLEAR_VALUE clearValue = default;

                    D3D12_BARRIER_LAYOUT initialLayout = D3D12_BARRIER_LAYOUT_UNDEFINED;

                    bool isClearValueCompatible = false;

                    switch (location.Resource.ResourceId)
                    {
                        case FGResourceId.Texture:
                            {
                                ref readonly FrameGraphTextureDesc texDesc = ref location.Resource.TextureDesc;

                                resDesc = new D3D12_RESOURCE_DESC1
                                {
                                    Dimension = texDesc.Dimension switch
                                    {
                                        FGTextureDimension._1D => D3D12_RESOURCE_DIMENSION_TEXTURE1D,
                                        FGTextureDimension._2D => D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                                        FGTextureDimension._3D => D3D12_RESOURCE_DIMENSION_TEXTURE3D,
                                        FGTextureDimension.Cube => D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                                    },
                                    Alignment = 0,
                                    Width = (ulong)texDesc.Width,
                                    Height = (uint)texDesc.Height,
                                    DepthOrArraySize = (ushort)texDesc.Depth,
                                    MipLevels = 1,
                                    Format = texDesc.Format.ToTextureFormat(),
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

                                    isClearValueCompatible = true;
                                }

                                if (FlagUtility.HasFlag(texDesc.Usage, FGTextureUsage.DepthStencil))
                                {
                                    resDesc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;

                                    clearValue = new D3D12_CLEAR_VALUE(resDesc.Format, 1.0f, 0xff);
                                    isClearValueCompatible = true;
                                }

                                if (FlagUtility.HasFlag(texDesc.Usage, FGTextureUsage.UnorderedAccess))
                                {
                                    resDesc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
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

                                if (FlagUtility.HasFlag(bufDesc.Usage, FGBufferUsage.UnorderedAccess))
                                {
                                    resDesc.Flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
                                }

                                break;
                            }
                    }

                    ID3D12Resource2* resourcePtr = null;
                    HRESULT r = D3D12MemAlloc.Allocator.CreateAliasingResource2(_device.Allocator, _resourcesMemory, (ulong)location.MemoryOffset, &resDesc, initialLayout, !isClearValueCompatible ? null : &clearValue, 0, null, resourceGuid, (void**)&resourcePtr);

                    if (r.FAILED)
                    {
                        _device.RHIDevice.FlushPendingMessages();
                        throw new NotImplementedException("Add error message");
                    }

                    if (location.Resource.DebugName != null)
                    {
                        ResourceUtility.SetResourceNameStack((ID3D12Resource*)resourcePtr, location.Resource.DebugName);
                    }

                    _allocatedResources[ResourceUtility.AsNRDResource(location.Resource)] = resourcePtr;
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

            _frameResourceData = null;
            _currentEventIndex = 0;
        }

        internal void CheckoutResourcesForPass(ID3D12GraphicsCommandList10* cmdList, int passIndex)
        {
            if (_frameResourceData != null && _currentEventIndex < _frameResourceData.Events.Length)
            {
                bool updatedResources = false;

                while (++_currentEventIndex < _frameResourceData.Events.Length)
                {
                    ref readonly FGResourceEvent @event = ref _frameResourceData.Events.DangerousGetReferenceAt(_currentEventIndex);
                    if (@event.PassIndex > passIndex)
                        break;

                    switch (@event.Action)
                    {
                        case FGResourceAction.Destroy:
                            {
                                NRDResource resource = ResourceUtility.AsNRDResource(@event.Resource);
                                if (resource.EncId == NRDResourceId.Texture)
                                {
                                    _device.BarrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_NONE, D3D12_BARRIER_ACCESS_NO_ACCESS, D3D12_BARRIER_LAYOUT_UNDEFINED);
                                }
                                else
                                {
                                    _device.BarrierManager.AddBufferBarrier(resource, D3D12_BARRIER_SYNC_NONE, D3D12_BARRIER_ACCESS_NO_ACCESS);
                                }

                                updatedResources = true;
                                break;
                            }
                    }
                }

                if (updatedResources)
                {
                    _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);
                }
            }
        }

        internal ID3D12Resource2* GetResource(NRDResource resource)
        {
            if (resource.IsExternal)
            {
                return resource.Id switch
                {
                    NRDResourceId.Texture => ((D3D12RHIBufferNative*)resource.Native)->Resource,
                    NRDResourceId.Buffer => ((D3D12RHITextureNative*)resource.Native)->Resource,
                    _ => throw new NotImplementedException(),
                };
            }

            if (_allocatedResources.TryGetValue(resource, out Ptr<ID3D12Resource2> ptr))
                return ptr.Pointer;

            return null;
        }

        internal FrameGraphBuffer FindFGBuffer(NRDResource resource)
        {
            if (resource.EncId != NRDResourceId.Buffer)
                return FrameGraphBuffer.Invalid;

            return _frameResourceData?.FindFGBuffer(resource.Index) ?? FrameGraphBuffer.Invalid;
        }

        internal FrameGraphTexture FindFGTexture(NRDResource resource)
        {
            if (resource.EncId != NRDResourceId.Texture)
                return FrameGraphTexture.Invalid;

            return _frameResourceData?.FindFGTexture(resource.Index) ?? FrameGraphTexture.Invalid;
        }

        internal void EnsureInitialized(NRDResource resource)
        {
            if (resource.IsExternal)
            {
                if (resource.Id == NRDResourceId.Buffer)
                    return;
                else
                {
                    D3D12RHITextureNative* native = (D3D12RHITextureNative*)resource.Native;
                    if (native->IsInitialized || !FlagUtility.HasEither(native->Base.Description.Usage, RHIResourceUsage.RenderTarget | RHIResourceUsage.DepthStencil))
                    {
                        native->IsInitialized = true;
                        return;
                    }

                    _pendingInitializes.Add(resource);
                }
            }

            if (_initializedResources.Contains(resource))
                return;

            _pendingInitializes.Add(resource);
        }

        internal void SetAsInitialized(NRDResource resource)
        {
            if (resource.IsExternal)
                return;

            _initializedResources.Add(resource);
        }

        internal void FlushPendingInits(ID3D12GraphicsCommandList10* cmdList)
        {
            if (_pendingInitializes.Count == 0)
                return;

            foreach (NRDResource resource in _pendingInitializes)
            {
                Debug.Assert(!_initializedResources.Contains(resource));

                if (resource.IsExternal)
                {
                    D3D12RHITextureNative* native = (D3D12RHITextureNative*)resource.Native;
                    _device.BarrierManager.AddTextureBarrier((ID3D12Resource*)native->Resource, D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
                }
                else if (resource.EncId == NRDResourceId.Texture)
                {
                    FrameGraphTexture fgTexture = FindFGTexture(resource);
                    ref readonly FrameGraphTextureDesc desc = ref fgTexture.Description;

                    if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.RenderTarget))
                    {
                        _device.BarrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
                    }
                    else if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.DepthStencil))
                    {
                        _device.BarrierManager.AddTextureBarrier(resource, D3D12_BARRIER_SYNC_DEPTH_STENCIL, D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE, D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
                    }
                }
            }

            _device.BarrierManager.FlushBarriers(cmdList, BarrierFlushTypes.Texture);

            foreach (NRDResource resource in _pendingInitializes)
            {
                if (resource.IsExternal)
                {
                    D3D12RHITextureNative* native = (D3D12RHITextureNative*)resource.Native;
                    ref readonly RHITextureDescription desc = ref native->Base.Description;

                    if (FlagUtility.HasFlag(desc.Usage, RHIResourceUsage.RenderTarget))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource, cmdList, D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
#endif

                        Color color = new Color(0.0f);
                        cmdList->ClearRenderTargetView(_device.RTVDescriptorHeap.GetDescriptorHandle(resource), (float*)&color, 0, null);
                    }
                    else if (FlagUtility.HasFlag(desc.Usage, RHIResourceUsage.DepthStencil))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource, cmdList, D3D12_BARRIER_SYNC_DEPTH_STENCIL, D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE, D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
#endif

                        cmdList->ClearDepthStencilView(_device.DSVDescriptorHeap.GetDescriptorHandle(resource), D3D12_CLEAR_FLAG_DEPTH | D3D12_CLEAR_FLAG_STENCIL, 1.0f, 0xff, 0, null);
                    }

                    native->IsInitialized = true;
                }
                else if (resource.EncId == NRDResourceId.Texture)
                {
                    FrameGraphTexture fgTexture = FindFGTexture(resource);
                    ref readonly FrameGraphTextureDesc desc = ref fgTexture.Description;

                    if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.RenderTarget))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource, cmdList, D3D12_BARRIER_SYNC_RENDER_TARGET, D3D12_BARRIER_ACCESS_RENDER_TARGET, D3D12_BARRIER_LAYOUT_RENDER_TARGET);
#endif

                        Color color = new Color(0.0f);
                        cmdList->ClearRenderTargetView(_device.RTVDescriptorHeap.GetDescriptorHandle(resource), (float*)&color, 0, null);
                    }
                    else if (FlagUtility.HasFlag(desc.Usage, FGTextureUsage.DepthStencil))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource, cmdList, D3D12_BARRIER_SYNC_DEPTH_STENCIL, D3D12_BARRIER_ACCESS_DEPTH_STENCIL_WRITE, D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE);
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
                            Dimension = texDesc.Dimension switch
                            {
                                FGTextureDimension._1D => D3D12_RESOURCE_DIMENSION_TEXTURE1D,
                                FGTextureDimension._2D => D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                                FGTextureDimension._3D => D3D12_RESOURCE_DIMENSION_TEXTURE3D,
                                FGTextureDimension.Cube => D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                            },
                            Alignment = 0,
                            Width = (ulong)texDesc.Width,
                            Height = (uint)texDesc.Height,
                            DepthOrArraySize = (ushort)texDesc.Depth,
                            MipLevels = 1,
                            Format = texDesc.Format.ToTextureFormat(),
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
            D3D12_RESOURCE_DESC1 resDesc = new D3D12_RESOURCE_DESC1
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
