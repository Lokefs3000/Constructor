using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Memory;
using Primary.Profiling;
using Primary.Rendering.Commands;
using Primary.Rendering.Resources;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using Primary.Threading;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using D3D12MemAlloc = Interop.D3D12MemAlloc;

namespace Primary.Rendering.Playback.D3D12
{
    internal unsafe sealed class PlaybackResourceManager : IDisposable, IJob
    {
        private readonly PlaybackDevice _device;
        private readonly PlaybackExecutor _executor;

        private Ptr<D3D12MemAlloc.Allocation> _resourcePool;
        private ResourceMetadata[] _resourceArray;

        private int _resourcePoolSize;
        private int _resourceArrayMax;

        private CompiledRenderPlan? _renderPlan;

        private bool _disposedValue;

        internal PlaybackResourceManager(PlaybackDevice device, PlaybackExecutor executor)
        {
            _device = device;
            _executor = executor;

            _resourcePool = Ptr<D3D12MemAlloc.Allocation>.Null;
            _resourceArray = [];

            _resourcePoolSize = 0;
            _resourceArrayMax = 0;

            _renderPlan = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (ResourceMetadata metadata in _resourceArray)
                {
                    if (!metadata.Aliased.IsNull)
                    {
                        metadata.Aliased.Pointer->Release();
                    }
                }

                Array.Clear(_resourceArray);

                if (!_resourcePool.IsNull)
                {
                    _resourcePool.Pointer->Base.Release();
                    _resourcePool = Ptr<D3D12MemAlloc.Allocation>.Null;
                }

                _disposedValue = true;
            }
        }

        ~PlaybackResourceManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void SetJobArguments(CompiledRenderPlan renderPlan)
        {
            _renderPlan = renderPlan;
        }

        void IJob.Execute()
        {
            using (new ProfilingScope("AllocResources"))
            {
                Debug.Assert(_renderPlan != null);

                if (_resourceArrayMax > 0)
                {
                    for (int i = 0; i < _resourceArrayMax; ++i)
                    {
                        ref ResourceMetadata resource = ref _resourceArray[i];
                        if (!resource.Aliased.IsNull)
                        {
                            resource.Aliased.Pointer->Release();
                        }

                        resource = default;
                    }
                }

                if (_renderPlan.Resources.Count == 0 || _renderPlan.RequiredResourceMemory == 0)
                {
                    return;
                }

                D3D12MemAlloc.Allocator* allocator = _device.NativeDevice.Allocator;
                ID3D12Device14* device = *_device.NativeDevice.Device.GetAddressOf();

                if (_resourcePool.IsNull || _resourcePoolSize < _renderPlan.RequiredResourceMemory)
                {
                    if (!_resourcePool.IsNull)
                    {
                        _resourcePool.Pointer->Base.Release();
                        _resourcePool = Ptr<D3D12MemAlloc.Allocation>.Null;
                    }

                    int alignment = (int)BitOperations.RoundUpToPowerOf2(0xffff);
                    int roundedUpSize = MemoryHelper.AlignUp(_renderPlan.RequiredResourceMemory, alignment);

                    D3D12MemAlloc.ALLOCATION_DESC allocDesc = new D3D12MemAlloc.ALLOCATION_DESC
                    {
                        Flags = D3D12MemAlloc.ALLOCATION_FLAGS.ALLOCATION_FLAG_CAN_ALIAS | D3D12MemAlloc.ALLOCATION_FLAGS.ALLOCATION_FLAG_COMMITTED,
                        HeapType = HeapType.Default,
                        ExtraHeapFlags = HeapFlags.AllowAllBuffersAndTextures | HeapFlags.AllowShaderAtomics
                    };

                    ResourceAllocationInfo allocInfo = new ResourceAllocationInfo
                    {
                        Alignment = (ulong)alignment,
                        SizeInBytes = (ulong)roundedUpSize
                    };

                    D3D12MemAlloc.Allocation* pool = null;
                    HResult hr = D3D12MemAlloc.Allocator.AllocateMemory(allocator, &allocDesc, &allocInfo, &pool);

                    if (hr.IsFailure)
                    {
                        EngLog.Render.Fatal("Failed to create allocation to pool for frame graph resources");
                        EngLog.Render.Fatal("    Attempted size: {s}b", roundedUpSize);
                        EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                        _device.NativeDevice.FlushPendingMessages();
                        EngLog.ThrowFatal();
                    }

                    _resourcePool = pool;
                    _resourcePoolSize = roundedUpSize;
                }

                ROList<RenderResourceInfo> resources = _renderPlan.Resources;
                if (_resourceArray.Length < _renderPlan.TotalResourceCount)
                {
                    int roundedUpSize = MemoryHelper.AlignUp(resources.Count, 32);
                    _resourceArray = new ResourceMetadata[roundedUpSize];
                }

                for (int i = 0; i < resources.Count; ++i)
                {
                    RenderResourceInfo resourceInfo = resources[i];
                    ref ResourceMetadata metadata = ref _resourceArray[i];

                    Debug.Assert(metadata.RHI == null && metadata.Aliased.IsNull);

                    if (resourceInfo.ImportedResource != null)
                    {
                        metadata = new ResourceMetadata(Ptr<ID3D12Resource2>.Null, resourceInfo.ImportedResource);
                    }
                    else
                    {
                        Debug.Assert(resourceInfo.ByteOffset >= 0 && resourceInfo.SizeInBytes > 0);
                        Debug.Assert(resourceInfo.DescriptionIndex != -1);

                        ref readonly GenericResourceDesc sourceResourceDesc = ref _renderPlan.GetDescription(resourceInfo.DescriptionIndex);

                        ResourceDesc1 resourceDesc = default;

                        ClearValue clearValue = default;
                        bool shouldHaveClearValue = false;

                        if (sourceResourceDesc.Type == FGResourceType.Buffer)
                        {
                            FGResourceDesc.Buffer bufferDesc = sourceResourceDesc.Resource.BufferDesc;
                            resourceDesc = new ResourceDesc1
                            {
                                Dimension = ResourceDimension.Buffer,
                                Alignment = 0,
                                Width = (ulong)resourceInfo.SizeInBytes,
                                Height = 1,
                                DepthOrArraySize = 1,
                                MipLevels = 1,
                                Format = Format.FormatUnknown,
                                SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                                Layout = TextureLayout.LayoutRowMajor,
                                Flags = ResourceFlags.AllowUnorderedAccess,
                                SamplerFeedbackMipRegion = new MipRegion { Width = 0, Height = 0, Depth = 0 }
                            };
                        }
                        else
                        {
                            FGResourceDesc.Texture textureDesc = sourceResourceDesc.Resource.TextureDesc;
                            resourceDesc = new ResourceDesc1
                            {
                                Dimension = textureDesc.Dimension switch
                                {
                                    FGTextureDimension.Tex2D => ResourceDimension.Texture2D,
                                    FGTextureDimension.Tex3D => ResourceDimension.Texture3D,
                                    FGTextureDimension.Cube => ResourceDimension.Texture2D,
                                    FGTextureDimension.Tex2DArray => ResourceDimension.Texture2D,
                                    FGTextureDimension.CubeArray => ResourceDimension.Texture2D,
                                    _ => throw new NotImplementedException(),
                                },
                                Alignment = 0,
                                Width = (ulong)textureDesc.Width,
                                Height = (uint)textureDesc.Height,
                                DepthOrArraySize = (ushort)((textureDesc.Dimension == FGTextureDimension.Cube || textureDesc.Dimension == FGTextureDimension.CubeArray) ? 6 * textureDesc.DepthOrArraySize : textureDesc.DepthOrArraySize),
                                MipLevels = (ushort)textureDesc.MipCount,
                                Format = Format.FormatUnknown,
                                SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                                Layout = TextureLayout.LayoutUnknown,
                                Flags = ResourceFlags.None,
                                SamplerFeedbackMipRegion = new MipRegion { Width = 0, Height = 0, Depth = 0 }
                            };

                            if (textureDesc.Target.HasFlags(FGTextureTarget.RenderTarget))
                            {
                                resourceDesc.Format = ConversionHelper.ToRenderTargetFormat(textureDesc.Format);
                                resourceDesc.Flags |= ResourceFlags.AllowRenderTarget;

                                clearValue = new ClearValue(resourceDesc.Format);
                                *(Color*)clearValue.Anonymous.Color = Color.TransparentBlack;
                                shouldHaveClearValue = true;
                            }
                            else if (textureDesc.Target.HasFlags(FGTextureTarget.DepthStencil))
                            {
                                resourceDesc.Format = ConversionHelper.ToDepthStencilFormat(textureDesc.Format);
                                resourceDesc.Flags |= ResourceFlags.AllowDepthStencil;

                                clearValue = new ClearValue(resourceDesc.Format);
                                clearValue.DepthStencil = new DepthStencilValue(1.0f, 0xff);
                                shouldHaveClearValue = true;
                            }

                            if (textureDesc.Target.HasFlags(FGTextureTarget.RandomReadWrite))
                                resourceDesc.Flags |= ResourceFlags.AllowUnorderedAccess;
                        }

                        ID3D12Resource2* resource = null;
                        HResult hr = D3D12MemAlloc.Allocator.CreateAliasingResource2(
                            allocator,
                            _resourcePool.Pointer,
                            (ulong)resourceInfo.ByteOffset,
                            &resourceDesc,
                            BarrierLayout.Undefined,
                            shouldHaveClearValue ? &clearValue : null,
                            0,
                            null,
                            SilkMarshal.GuidPtrOf<ID3D12Resource2>(),
                            (void**)&resource);

                        if (hr.IsFailure)
                        {
                            EngLog.Render.Fatal("Failed to create aliased resource specified by frame graph");
                            EngLog.Render.Fatal("    Resource desc: {");
                            EngLog.Render.Fatal("       Dimension: {v}", resourceDesc.Dimension);
                            EngLog.Render.Fatal("       Alignment: {v}", resourceDesc.Alignment);
                            EngLog.Render.Fatal("       Width: {v}", resourceDesc.Width);
                            EngLog.Render.Fatal("       Height: {v}", resourceDesc.Height);
                            EngLog.Render.Fatal("       DepthOrArraySize: {v}", resourceDesc.DepthOrArraySize);
                            EngLog.Render.Fatal("       MipLevels: {v}", resourceDesc.MipLevels);
                            EngLog.Render.Fatal("       Format: {v}", resourceDesc.Format);
                            EngLog.Render.Fatal("       SampleDesc: {{ count:{v0}, quality:{v1} }}", resourceDesc.SampleDesc.Count, resourceDesc.SampleDesc.Quality);
                            EngLog.Render.Fatal("       Layout: {v}", resourceDesc.Layout);
                            EngLog.Render.Fatal("       Flags: {v}", resourceDesc.Flags);
                            EngLog.Render.Fatal("       SamplerFeedbackMipRegion: {{ w:{v0}, h:{v1}, d:{v2} }}", resourceDesc.SamplerFeedbackMipRegion.Width, resourceDesc.SamplerFeedbackMipRegion.Height, resourceDesc.SamplerFeedbackMipRegion.Depth);
                            EngLog.Render.Fatal("    }");
                            EngLog.Render.Fatal("    HResult value: {v:x8}", hr.Value);

                            _device.NativeDevice.FlushPendingMessages();
                            EngLog.ThrowFatal();
                        }

                        if (sourceResourceDesc.Name != null)
                        {
                            if (!ResourceHelper.SetResourceName(resource, sourceResourceDesc.Name))
                            {
                                EngLog.Render.Debug("Failed to set resource name '{n}'", sourceResourceDesc.Name);
                            }
                        }

                        metadata = new ResourceMetadata(resource, null);
                    }
                }

                _resourceArrayMax = _renderPlan.TotalResourceCount;
            }
        }

        internal ID3D12Resource2* GetResourcePointer(int resourceIndex)
        {
            Debug.Assert(_renderPlan != null);

            int index = _renderPlan.ResourceIndices[resourceIndex];

            ref ResourceMetadata metadata = ref _resourceArray[index];
            return metadata.RHI != null ?
                (metadata.RHI is D3D12RHIBuffer buffer ?
                    ((D3D12RHIBufferNative*)buffer.GetAsNative())->Resource :
                    ((D3D12RHITextureNative*)Unsafe.As<D3D12RHITexture>(metadata.RHI).GetAsNative())->Resource) :
                metadata.Aliased.Pointer;
        }

        internal ID3D12Resource2* GetResourcePointer(ResourceCmdData resourceData) => GetResourcePointer(resourceData.Index);

        private readonly record struct ResourceMetadata(Ptr<ID3D12Resource2> Aliased, RHIResource? RHI);
    }
}
