using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Profiling;
using Primary.Rendering.Pass;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal sealed unsafe class ResourceManager : IDisposable
    {
        private readonly NRDDevice _device;

        private D3D12MemAlloc.Pool* _resourcesPool;
        private D3D12MemAlloc.Allocation* _resourcesMemory;
        private int _resourceMemorySize;

        private Dictionary<NRDResource, ComPtr<ID3D12Resource2>> _allocatedResources;
        private HashSet<NRDResource> _initializedResources;

        private HashSet<NRDResource> _pendingInitializes;

        private FrameGraphResources? _frameResourceData;
        private int _currentEventIndex;

        private bool _disposedValue;

        internal ResourceManager(NRDDevice device)
        {
            _device = device;

            _resourcesPool = null;
            _resourcesMemory = null;
            _resourceMemorySize = 0;

            _allocatedResources = new Dictionary<NRDResource, ComPtr<ID3D12Resource2>>();
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
                if (_resourcesPool != null)
                    _resourcesPool->Base.Release();

                _resourcesPool = null;
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

        internal bool PrepareForExecution(FrameGraphResources resources)
        {
            using (new ProfilingScope("Resources"))
            {
                FreeReferencedResources();

                _frameResourceData = resources;
                _currentEventIndex = 0;

                Debug.Assert(_allocatedResources.Count == 0);

                const int SafetyNetSize = Silk.NET.Direct3D12.D3D12.DefaultResourcePlacementAlignment;

                int memoryUsageRequired = resources.HighestMemoryUsage + SafetyNetSize;
                memoryUsageRequired = (memoryUsageRequired + (-memoryUsageRequired & 255));

                if (_resourceMemorySize < memoryUsageRequired)
                {
                    if (_resourcesMemory != null)
                    {
                        _resourcesMemory->Base.Release();
                        _resourcesMemory = null;

                        _resourcesPool->Base.Release();
                        _resourcesPool = null;
                    }

                    {
                        D3D12MemAlloc.POOL_DESC poolDesc = new D3D12MemAlloc.POOL_DESC
                        {
                            Flags = D3D12MemAlloc.POOL_FLAGS.POOL_FLAG_ALGORITHM_LINEAR,
                            HeapProperties = new HeapProperties
                            {
                                Type = HeapType.Default
                            },
                            HeapFlags = HeapFlags.AllowAllBuffersAndTextures,
                        };

                        D3D12MemAlloc.Pool* tempAllocPtr = null;

                        int r = D3D12MemAlloc.Allocator.CreatePool(_device.Allocator, &poolDesc, &tempAllocPtr);
                        if (r != 0)
                        {
                            _device.RHIDevice.FlushPendingMessages();
                            throw new NotImplementedException("Add error message");
                        }

                        _resourcesPool = tempAllocPtr;
                    }

                    {
                        D3D12MemAlloc.ALLOCATION_DESC allocDesc = new D3D12MemAlloc.ALLOCATION_DESC
                        {
                            Flags = D3D12MemAlloc.ALLOCATION_FLAGS.ALLOCATION_FLAG_CAN_ALIAS,
                            HeapType = HeapType.Default,
                            ExtraHeapFlags = HeapFlags.AllowAllBuffersAndTextures,
                            CustomPool = _resourcesPool
                        };

                        ResourceAllocationInfo resAllocDesc = new ResourceAllocationInfo
                        {
                            SizeInBytes = (ulong)memoryUsageRequired,
                            Alignment = 0 // this was once upon a time '256'
                        };

                        D3D12MemAlloc.Allocation* tempAllocPtr = null;

                        int r = D3D12MemAlloc.Allocator.AllocateMemory(_device.Allocator, &allocDesc, &resAllocDesc, &tempAllocPtr);
                        if (r != 0)
                        {
                            _device.RHIDevice.FlushPendingMessages();
                            throw new NotImplementedException("Add error message");
                        }

                        _resourcesMemory = tempAllocPtr;
                    }

                    _resourceMemorySize = memoryUsageRequired;
                }

                //TODO: add support for a placed resource memory as the alias buffer aswell
                ulong heapOffset = D3D12MemAlloc.Allocation.GetOffset(_resourcesMemory);
                Debug.Assert(heapOffset == 0);

                ResourceFlags startingFlags = Unsafe.As<D3D12RHIDevice>(_device.RHIDevice).Setup.UseTightAlignment ? ResourceFlags.UseTightAlignment : ResourceFlags.None;

                foreach (ref readonly FGResourceLocation location in resources.Locations)
                {
                    ResourceDesc1 resDesc = default;
                    ClearValue clearValue = default;

                    BarrierLayout initialLayout = BarrierLayout.Undefined;

                    bool isClearValueCompatible = false;

                    switch (location.Resource.ResourceId)
                    {
                        case FGResourceId.Texture:
                            {
                                ref readonly FrameGraphTextureDesc texDesc = ref location.Resource.TextureDesc;

                                Debug.Assert(texDesc.Width > 0 && texDesc.Height > 0 && texDesc.Depth > 0);

                                resDesc = new ResourceDesc1
                                {
                                    Dimension = texDesc.Dimension switch
                                    {
                                        FGTextureDimension._1D => ResourceDimension.Texture1D,
                                        FGTextureDimension._2D => ResourceDimension.Texture2D,
                                        FGTextureDimension._3D => ResourceDimension.Texture3D,
                                        FGTextureDimension.Cube => ResourceDimension.Texture2D,
                                    },
                                    Alignment = 0,
                                    Width = (ulong)texDesc.Width,
                                    Height = (uint)texDesc.Height,
                                    DepthOrArraySize = (ushort)texDesc.Depth,
                                    MipLevels = 1,
                                    Format = texDesc.Format.ToTextureFormat(),
                                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                                    Layout = TextureLayout.LayoutUnknown,
                                    Flags = startingFlags,
                                    SamplerFeedbackMipRegion = default
                                };

                                if (Flags.HasFlag(texDesc.Usage, FGTextureUsage.RenderTarget))
                                {
                                    resDesc.Flags |= ResourceFlags.AllowRenderTarget;

                                    clearValue = new ClearValue(resDesc.Format);
                                    *((Color*)clearValue.Anonymous.Color) = Color.Black;

                                    isClearValueCompatible = true;
                                }

                                if (Flags.HasFlag(texDesc.Usage, FGTextureUsage.DepthStencil))
                                {
                                    resDesc.Flags |= ResourceFlags.AllowDepthStencil;

                                    clearValue = new ClearValue(resDesc.Format);
                                    clearValue.DepthStencil = new DepthStencilValue(1.0f, 0xff);

                                    isClearValueCompatible = true;
                                }

                                if (Flags.HasFlag(texDesc.Usage, FGTextureUsage.UnorderedAccess))
                                {
                                    resDesc.Flags |= ResourceFlags.AllowUnorderedAccess;
                                }

                                break;
                            }
                        case FGResourceId.Buffer:
                            {
                                ref readonly FrameGraphBufferDesc bufDesc = ref location.Resource.BufferDesc;

                                Debug.Assert(bufDesc.Width > 0);

                                resDesc = new ResourceDesc1
                                {
                                    Dimension = ResourceDimension.Buffer,
                                    Alignment = 0,
                                    Width = bufDesc.Width,
                                    Height = 1,
                                    DepthOrArraySize = 1,
                                    MipLevels = 1,
                                    Format = Format.FormatUnknown,
                                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                                    Layout = TextureLayout.LayoutRowMajor,
                                    Flags = startingFlags,
                                    SamplerFeedbackMipRegion = default
                                };

                                if (Flags.HasFlag(bufDesc.Usage, FGBufferUsage.UnorderedAccess))
                                {
                                    resDesc.Flags |= ResourceFlags.AllowUnorderedAccess;
                                }

                                break;
                            }
                    }

                    ComPtr<ID3D12Resource2> resource = new ComPtr<ID3D12Resource2>();
                    HResult r = D3D12MemAlloc.Allocator.CreateAliasingResource2(_device.Allocator, _resourcesMemory, (ulong)location.MemoryOffset, &resDesc, initialLayout, !isClearValueCompatible ? null : &clearValue, 0, null, SilkMarshal.GuidPtrOf<ID3D12Resource2>(), (void**)resource.GetAddressOf());

                    if (r.IsFailure)
                    {
                        _device.RHIDevice.FlushPendingMessages();
                        return false;
                    }

                    if (location.Resource.DebugName != null)
                    {
                        ResourceHelper.SetResourceName(ref resource.Get(), location.Resource.DebugName);
                    }

                    _allocatedResources[ResourceUtility.AsNRDResource(location.Resource)] = resource;
                }

                return true;
            }
        }

        internal void FreeReferencedResources()
        {
            foreach (var kvp in _allocatedResources)
            {
                kvp.Value.Dispose();
            }

            _allocatedResources.Clear();

            _initializedResources.Clear();
            _pendingInitializes.Clear();

            _frameResourceData = null;
            _currentEventIndex = 0;
        }

        internal void CheckoutResourcesForPass(ref ID3D12GraphicsCommandList10 cmdList, int passIndex)
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
                                    _device.BarrierManager.AddTextureBarrier(resource, BarrierSync.None, BarrierAccess.NoAccess, BarrierLayout.Undefined);
                                }
                                else
                                {
                                    _device.BarrierManager.AddBufferBarrier(resource, BarrierSync.None, BarrierAccess.NoAccess);
                                }

                                updatedResources = true;
                                break;
                            }
                    }
                }

                if (updatedResources)
                {
                    _device.BarrierManager.FlushBarriers(ref cmdList, BarrierFlushTypes.Buffer | BarrierFlushTypes.Texture);
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
                    NRDResourceId.Readback => ((D3D12RHIReadbackNative*)resource.Native)->Resource,
                    _ => throw new NotImplementedException(),
                };
            }

            if (_allocatedResources.TryGetValue(resource, out ComPtr<ID3D12Resource2> ptr))
                return (ID3D12Resource2*)Unsafe.AsPointer(ref ptr.Get());

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
                    if (native->IsInitialized || !Flags.HasEither(native->Base.Description.Usage, RHIResourceUsage.RenderTarget | RHIResourceUsage.DepthStencil))
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
                ((D3D12RHITextureNative*)resource.Native)->IsInitialized = true;
            else
                _initializedResources.Add(resource);
        }

        internal bool IsInitialized(NRDResource resource)
        {
            if (resource.Id == NRDResourceId.Buffer)
                return true;

            return resource.IsExternal ? ((D3D12RHITextureNative*)resource.Native)->IsInitialized : _initializedResources.Contains(resource);
        }

        internal void FlushPendingInits(ref ID3D12GraphicsCommandList10 cmdList)
        {
            if (_pendingInitializes.Count == 0)
                return;

            foreach (NRDResource resource in _pendingInitializes)
            {
                Debug.Assert(!_initializedResources.Contains(resource));

                if (resource.IsExternal)
                {
                    D3D12RHITextureNative* native = (D3D12RHITextureNative*)resource.Native;
                    _device.BarrierManager.AddTextureBarrier((ID3D12Resource*)native->Resource, BarrierSync.RenderTarget, BarrierAccess.RenderTarget, BarrierLayout.RenderTarget);
                }
                else if (resource.EncId == NRDResourceId.Texture)
                {
                    FrameGraphTexture fgTexture = FindFGTexture(resource);
                    ref readonly FrameGraphTextureDesc desc = ref fgTexture.Description;

                    if (Flags.HasFlag(desc.Usage, FGTextureUsage.RenderTarget))
                    {
                        _device.BarrierManager.AddTextureBarrier(resource, BarrierSync.RenderTarget, BarrierAccess.RenderTarget, BarrierLayout.RenderTarget);
                    }
                    else if (Flags.HasFlag(desc.Usage, FGTextureUsage.DepthStencil))
                    {
                        _device.BarrierManager.AddTextureBarrier(resource, BarrierSync.DepthStencil, BarrierAccess.DepthStencilWrite, BarrierLayout.DepthStencilWrite);
                    }
                }
            }

            _device.BarrierManager.FlushBarriers(ref cmdList, BarrierFlushTypes.Texture);

            foreach (NRDResource resource in _pendingInitializes)
            {
                if (resource.IsExternal)
                {
                    D3D12RHITextureNative* native = (D3D12RHITextureNative*)resource.Native;
                    ref readonly RHITextureDescription desc = ref native->Base.Description;

                    if (Flags.HasFlag(desc.Usage, RHIResourceUsage.RenderTarget))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource, ref cmdList, BarrierSync.RenderTarget, BarrierAccess.RenderTarget, BarrierLayout.RenderTarget);
#endif

                        Color color = new Color(0.0f);
                        cmdList.ClearRenderTargetView(_device.RTVDescriptorHeap.GetDescriptorHandle(resource), (float*)&color, 0, null);
                    }
                    else if (Flags.HasFlag(desc.Usage, RHIResourceUsage.DepthStencil))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource, ref cmdList, BarrierSync.DepthStencil, BarrierAccess.DepthStencilWrite, BarrierLayout.DepthStencilWrite);
#endif

                        cmdList.ClearDepthStencilView(_device.DSVDescriptorHeap.GetDescriptorHandle(resource), ClearFlags.Depth | ClearFlags.Stencil, 1.0f, 0xff, 0, null);
                    }

                    native->IsInitialized = true;
                }
                else if (resource.EncId == NRDResourceId.Texture)
                {
                    FrameGraphTexture fgTexture = FindFGTexture(resource);
                    ref readonly FrameGraphTextureDesc desc = ref fgTexture.Description;

                    if (Flags.HasFlag(desc.Usage, FGTextureUsage.RenderTarget))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource, ref cmdList, BarrierSync.RenderTarget, BarrierAccess.RenderTarget, BarrierLayout.RenderTarget);
#endif

                        Color color = new Color(0.0f);
                        cmdList.ClearRenderTargetView(_device.RTVDescriptorHeap.GetDescriptorHandle(resource), (float*)&color, 0, null);
                    }
                    else if (Flags.HasFlag(desc.Usage, FGTextureUsage.DepthStencil))
                    {
#if DEBUG
                        _device.BarrierManager.DbgEnsureState(resource, ref cmdList, BarrierSync.DepthStencil, BarrierAccess.DepthStencilWrite, BarrierLayout.DepthStencilWrite);
#endif

                        cmdList.ClearDepthStencilView(_device.DSVDescriptorHeap.GetDescriptorHandle(resource), ClearFlags.Depth | ClearFlags.Stencil, 1.0f, 0xff, 0, null);
                    }
                }

                _initializedResources.Add(resource);
            }

            _pendingInitializes.Clear();
        }

        internal static ResourceDesc1 GetResourceDescription(NRDDevice device, FrameGraphResource resource)
        {
            ResourceDesc1 resDesc = default;

            switch (resource.ResourceId)
            {
                case FGResourceId.Texture:
                    {
                        ref readonly FrameGraphTextureDesc texDesc = ref resource.TextureDesc;

                        resDesc = new ResourceDesc1
                        {
                            Dimension = texDesc.Dimension switch
                            {
                                FGTextureDimension._1D => ResourceDimension.Texture1D,
                                FGTextureDimension._2D => ResourceDimension.Texture2D,
                                FGTextureDimension._3D => ResourceDimension.Texture3D,
                                FGTextureDimension.Cube => ResourceDimension.Texture2D,
                            },
                            Alignment = 0,
                            Width = (ulong)texDesc.Width,
                            Height = (uint)texDesc.Height,
                            DepthOrArraySize = (ushort)texDesc.Depth,
                            MipLevels = 1,
                            Format = texDesc.Format.ToTextureFormat(),
                            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                            Layout = TextureLayout.LayoutUnknown,
                            Flags = Unsafe.As<D3D12RHIDevice>(device.RHIDevice).Setup.UseTightAlignment ? ResourceFlags.UseTightAlignment : ResourceFlags.None
                        };

                        if (Flags.HasFlag(texDesc.Usage, FGTextureUsage.RenderTarget))
                        {
                            resDesc.Flags |= ResourceFlags.AllowRenderTarget;
                        }

                        if (Flags.HasFlag(texDesc.Usage, FGTextureUsage.DepthStencil))
                        {
                            resDesc.Flags |= ResourceFlags.AllowDepthStencil;
                        }

                        if (Flags.HasFlag(texDesc.Usage, FGTextureUsage.UnorderedAccess))
                        {
                            resDesc.Flags |= ResourceFlags.AllowUnorderedAccess;
                        }

                        break;
                    }
                case FGResourceId.Buffer:
                    {
                        ref readonly FrameGraphBufferDesc bufDesc = ref resource.BufferDesc;

                        resDesc = new ResourceDesc1
                        {
                            Dimension = ResourceDimension.Buffer,
                            Alignment = 0,
                            Width = bufDesc.Width,
                            Height = 1,
                            DepthOrArraySize = 1,
                            MipLevels = 1,
                            Format = Format.FormatUnknown,
                            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                            Layout = TextureLayout.LayoutRowMajor,
                            Flags = Unsafe.As<D3D12RHIDevice>(device.RHIDevice).Setup.UseTightAlignment ? ResourceFlags.UseTightAlignment : ResourceFlags.None
                        };

                        break;
                    }
            }

            return resDesc;
        }

        internal static ResourceDesc1 GetBufferDescription(NRDDevice device, int width)
        {
            ResourceDesc1 resDesc = new ResourceDesc1
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = (ulong)width,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.FormatUnknown,
                SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                Layout = TextureLayout.LayoutRowMajor,
                Flags = Unsafe.As<D3D12RHIDevice>(device.RHIDevice).Setup.UseTightAlignment ? ResourceFlags.UseTightAlignment : ResourceFlags.None
            };

            return resDesc;
        }

        internal static ResourceDesc1 GetTextureDescription(NRDDevice device, FrameGraphTexture texture)
        {
            FrameGraphTextureDesc texDesc = texture.Description;

            ResourceDesc1 resDesc = new ResourceDesc1
            {
                Dimension = texDesc.Dimension switch
                {
                    FGTextureDimension._1D => ResourceDimension.Texture1D,
                    FGTextureDimension._2D => ResourceDimension.Texture2D,
                    FGTextureDimension._3D => ResourceDimension.Texture3D,
                    FGTextureDimension.Cube => ResourceDimension.Texture2D,
                },
                Alignment = 0,
                Width = (ulong)texDesc.Width,
                Height = (uint)texDesc.Height,
                DepthOrArraySize = (ushort)texDesc.Depth,
                MipLevels = 1,
                Format = texDesc.Format.ToTextureFormat(),
                SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                Layout = TextureLayout.LayoutUnknown,
                Flags = ResourceFlags.None,
                SamplerFeedbackMipRegion = default
            };

            return resDesc;
        }
    }
}
