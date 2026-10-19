using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Collections.Extensions;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Pooling;
using Primary.Profiling;
using Primary.Rendering.Passes;
using Primary.RHI;

namespace Primary.Rendering.Resources
{
    public sealed class ResourceManager
    {
        private readonly FrameGraph _frameGraph;

        private readonly ObjectPool<ResourceVersion> _versionPool;

        private readonly List<FGBufferDesc> _bufferDescs;
        private readonly List<FGTextureDesc> _textureDescs;

        private readonly List<ImportedResourceData> _importedResources;
        private readonly Dictionary<RHIResource, ResourceHandle> _importedResourceDict;

        private readonly List<ResourceEntry> _entries;

        private readonly HashSet<TextureHandle> _encounteredTextures;
        private readonly HashSet<RHIResource> _encounteredResources;

        private readonly LifetimeManager _lifetimeManager;

        internal ResourceManager(FrameGraph frameGraph)
        {
            _frameGraph = frameGraph;

            _versionPool = new ObjectPool<ResourceVersion>(ResourceVersion.GenericPoolPolicy.Default);

            _bufferDescs = new List<FGBufferDesc>();
            _textureDescs = new List<FGTextureDesc>();

            _importedResources = new List<ImportedResourceData>();
            _importedResourceDict = new Dictionary<RHIResource, ResourceHandle>();

            _entries = new List<ResourceEntry>();

            _encounteredTextures = new HashSet<TextureHandle>();
            _encounteredResources = new HashSet<RHIResource>();

            _lifetimeManager = new LifetimeManager(this);
        }

        internal void ClearPreviousFrame()
        {
            foreach (ResourceEntry entry in _entries)
            {
                if (entry.Version != null)
                    _versionPool.Return(entry.Version);
            }

            _bufferDescs.Clear();
            _textureDescs.Clear();

            _importedResources.Clear();
            _importedResourceDict.Clear();

            _encounteredTextures.Clear();
            _encounteredResources.Clear();

            _entries.Clear();
        }

        internal void ComputeResourceLifetimes(CompiledRenderPlan plan)
        {
            using (new ProfilingScope("ComputeLifetimes"))
            {
                _lifetimeManager.SetupForLifetimeEvaluation(_entries.Count);

                ROList<RenderEventInfo> events = plan.Events;
                for (int i = 0; i < events.Count; ++i)
                {
                    RenderEventInfo eventInfo = events[i];

                    if (eventInfo.EventType == RenderEventType.ExecuteDirect || eventInfo.EventType == RenderEventType.ExecuteCompute)
                    {
                        CompiledRenderPass compiledPass = eventInfo.RenderPass1!;
                        RenderPassIndex renderPass = compiledPass.RenderPass!;

                        foreach (ResourceHandle resource in renderPass.Resources)
                        {
                            if (!resource.IsImported)
                            {
                                _lifetimeManager.ExtendResourceLifetime(resource, i, eventInfo.EventType);
                            }
                        }

                        compiledPass.BarrierGroup.SetupForResources(_entries.Count);

                        foreach (ResourceHandle resourceToWrite in renderPass.Writes)
                        {
                            if (resourceToWrite.ResourceType == FGResourceType.Texture)
                            {
                                TextureHandle texture = resourceToWrite.AsTexture();
                                if (!texture.IsImported && _encounteredTextures.Add(texture))
                                {
                                    ref readonly FGTextureDesc textureDesc = ref GetDescription(texture);
                                    if (textureDesc.Target.HasFlags(FGTextureTarget.RenderTarget))
                                    {
                                        compiledPass.AddUninitializedRenderTarget(texture);
                                    }
                                    else if (textureDesc.Target.HasFlags(FGTextureTarget.DepthStencil))
                                    {
                                        compiledPass.AddUninitializedDepthStencil(texture);
                                    }
                                }
                            }
                        }
                    }
                    else if (eventInfo.EventType == RenderEventType.InsertBarrier)
                    {
                        _lifetimeManager.HandleLifetimesAtBarrier(i);
                    }
                }

                _lifetimeManager.FinishLifetimeEvaluation();
            }

            using (new ProfilingScope("Flush"))
            {
                plan.RequiredResourceMemory = _lifetimeManager.AllocatedMemory;

                plan.AllocateResourceIndices(_entries.Count);
                foreach (LifetimeIndex firstLifetime in _lifetimeManager.FirstLifetimes[.._entries.Count])
                {
                    if (firstLifetime.ResourceIndex == -1)
                        continue;

                    ResourceHandle handle = GetResourceHandle(firstLifetime.ResourceIndex);
                    Debug.Assert(!handle.IsImported);

                    GenericResourceDesc resourceDesc;
                    if (handle.ResourceType == FGResourceType.Buffer)
                    {
                        ref readonly FGBufferDesc bufferDesc = ref _bufferDescs.GetRefAtIndex(handle.LocalIndex);
                        resourceDesc = bufferDesc;
                    }
                    else
                    {
                        ref readonly FGTextureDesc textureDesc = ref _textureDescs.GetRefAtIndex(handle.LocalIndex);
                        resourceDesc = textureDesc;
                    }

                    ref readonly ResourceVirtualAllocInfo allocInfo = ref _lifetimeManager.ResourceVirtualAllocs[firstLifetime.ResourceIndex];
                    plan.AddResource(allocInfo.Allocation.MemoryOffset, allocInfo.SizeInBytes, allocInfo.ResourceIndex, resourceDesc);
                }

                foreach (ImportedResourceData importedResource in _importedResources)
                {
                    plan.AddResource(importedResource.Resource, importedResource.Index);
                    _encounteredResources.Add(importedResource.Resource);
                }

                plan.TotalResourceCount = plan.Resources.Count;
            }
        }

        internal void FlushPureResources(CompiledRenderPlan plan)
        {
            using (new ProfilingScope("FlushPureRes"))
            {
                plan.AllocateResourceIndices(_entries.Count, true);
                foreach (ImportedResourceData importedResource in _importedResources)
                {
                    if (importedResource.IsPure && _encounteredResources.Add(importedResource.Resource))
                        plan.AddResource(importedResource.Resource, importedResource.Index);
                }

                plan.TotalResourceCount = plan.Resources.Count;
            }
        }

        internal BufferHandle CreateBuffer(in FGBufferDesc bufferDesc)
        {
            ValidateBufferDesc(in bufferDesc);

            int index = _entries.Count;
            int descIndex = _bufferDescs.Count;

            ResourceAlignmentInfo alignmentInfo =
                Engine.GlobalSingleton.RenderingManager.PlaybackDevice.QueryForAlignment(in bufferDesc);

            _entries.Add(new ResourceEntry(descIndex, false, FGResourceType.Buffer, alignmentInfo, _versionPool.Get()));
            _bufferDescs.Add(bufferDesc);

            return new BufferHandle(new ResourceHandle(index, (ushort)descIndex, FGResourceType.Buffer, false));
        }

        internal TextureHandle CreateTexture(in FGTextureDesc textureDesc)
        {
            ValidateTextureDesc(in textureDesc);

            int index = _entries.Count;
            int descIndex = _textureDescs.Count;

            ResourceAlignmentInfo alignmentInfo =
                Engine.GlobalSingleton.RenderingManager.PlaybackDevice.QueryForAlignment(in textureDesc);

            _entries.Add(new ResourceEntry(descIndex, false, FGResourceType.Texture, alignmentInfo, _versionPool.Get()));
            _textureDescs.Add(textureDesc);

            return new TextureHandle(new ResourceHandle(index, (ushort)descIndex, FGResourceType.Texture, false));
        }

        internal BufferHandle ImportBuffer(in RHIBuffer buffer)
        {
            ref ResourceHandle handle = ref CollectionsMarshal.GetValueRefOrAddDefault(_importedResourceDict, buffer, out bool exists);
            if (exists)
                return handle.AsBuffer();

            int index = _entries.Count;
            int importIndex = _importedResources.Count;

            ref readonly RHIAlignmentInfo rhiAlignInfo = ref buffer.AlignmentInfo;
            ResourceAlignmentInfo alignmentInfo = new ResourceAlignmentInfo(rhiAlignInfo.Alignment, rhiAlignInfo.SizeInBytes);

            _entries.Add(new ResourceEntry(importIndex, true, FGResourceType.Buffer, alignmentInfo, _versionPool.Get()));
            _importedResources.Add(new ImportedResourceData(index, buffer, false));

            handle = new ResourceHandle(index, (ushort)importIndex, FGResourceType.Buffer, true);
            return handle.AsBuffer();
        }

        internal TextureHandle ImportTexture(in RHITexture texture)
        {
            ref ResourceHandle handle = ref CollectionsMarshal.GetValueRefOrAddDefault(_importedResourceDict, texture, out bool exists);
            if (exists)
                return handle.AsTexture();

            int index = _entries.Count;
            int importIndex = _importedResources.Count;

            ref readonly RHIAlignmentInfo rhiAlignInfo = ref texture.AlignmentInfo;
            ResourceAlignmentInfo alignmentInfo = new ResourceAlignmentInfo(rhiAlignInfo.Alignment, rhiAlignInfo.SizeInBytes);

            _entries.Add(new ResourceEntry(importIndex, true, FGResourceType.Texture, alignmentInfo, _versionPool.Get()));
            _importedResources.Add(new ImportedResourceData(index, texture, false));

            handle = new ResourceHandle(index, (ushort)importIndex, FGResourceType.Texture, true);
            return handle.AsTexture();
        }

        internal BufferHandle ImportBufferAsPure(in RHIBuffer buffer)
        {
            ref ResourceHandle handle = ref CollectionsMarshal.GetValueRefOrAddDefault(_importedResourceDict, buffer, out bool exists);
            if (exists)
            {
                ref ImportedResourceData data = ref _importedResources.AsSpan().DangerousGetReferenceAt(handle.LocalIndex);
                Guard.IsTrue(data.IsPure, "Attempting to re-import a buffer as pure is invalid");

                return handle.AsBuffer();
            }

            int index = _entries.Count;
            int importIndex = _importedResources.Count;

            ref readonly RHIAlignmentInfo rhiAlignInfo = ref buffer.AlignmentInfo;
            ResourceAlignmentInfo alignmentInfo = new ResourceAlignmentInfo(rhiAlignInfo.Alignment, rhiAlignInfo.SizeInBytes);

            _entries.Add(new ResourceEntry(importIndex, true, FGResourceType.Buffer, alignmentInfo, null));
            _importedResources.Add(new ImportedResourceData(index, buffer, true));

            handle = new ResourceHandle(index, (ushort)importIndex, FGResourceType.Buffer, true);
            return handle.AsBuffer();
        }

        internal TextureHandle ImportTextureAsPure(in RHITexture texture)
        {
            ref ResourceHandle handle = ref CollectionsMarshal.GetValueRefOrAddDefault(_importedResourceDict, texture, out bool exists);
            if (exists)
            {
                ref ImportedResourceData data = ref _importedResources.AsSpan().DangerousGetReferenceAt(handle.LocalIndex);
                Guard.IsTrue(data.IsPure, "Attempting to re-import a texture as pure is invalid");

                return handle.AsTexture();
            }

            int index = _entries.Count;
            int importIndex = _importedResources.Count;

            ref readonly RHIAlignmentInfo rhiAlignInfo = ref texture.AlignmentInfo;
            ResourceAlignmentInfo alignmentInfo = new ResourceAlignmentInfo(rhiAlignInfo.Alignment, rhiAlignInfo.SizeInBytes);

            _entries.Add(new ResourceEntry(importIndex, true, FGResourceType.Texture, alignmentInfo, null));
            _importedResources.Add(new ImportedResourceData(index, texture, true));

            handle = new ResourceHandle(index, (ushort)importIndex, FGResourceType.Texture, true);
            return handle.AsTexture();
        }

        internal ResourceAlignmentInfo EvaluateMemoryForResource(ResourceHandle resource) => _entries.GetRefAtIndex(resource.Index).AlignmentInfo;

        internal ref readonly FGBufferDesc GetDescription(in BufferHandle handle)
        {
            Debug.Assert(!handle.IsImported);
            return ref _bufferDescs.GetRefAtIndex(handle.LocalIndex);
        }

        internal ref readonly FGTextureDesc GetDescription(in TextureHandle handle)
        {
            Debug.Assert(!handle.IsImported);
            return ref _textureDescs.GetRefAtIndex(handle.LocalIndex);
        }

        internal RHIBuffer GetRHIResource(in BufferHandle handle) => (RHIBuffer)_importedResources.GetRefAtIndex(handle.LocalIndex).Resource;
        internal RHITexture GetRHIResource(in TextureHandle handle) => (RHITexture)_importedResources.GetRefAtIndex(handle.LocalIndex).Resource;

        internal ResourceHandle GetResourceHandle(int resourceIndex)
        {
            ResourceEntry entry = _entries[resourceIndex];
            return new ResourceHandle(resourceIndex, (ushort)entry.LocalIndex, entry.ResourceType, entry.IsImported);
        }

        internal ResourceHandle GetResourceHandle(RHIResource resource)
        {
            return _importedResourceDict[resource];
        }

        internal void AddRead(RenderPassIndex renderPass, ResourceHandle handle)
        {
            ResourceVersion version = _entries.AsSpan()[handle.Index].Version!;
            Guard.IsNotNull(version);

            if (version.CurrentWriter != null)
            {
                renderPass.AddDependency(version.CurrentWriter);
            }

            version.Read(renderPass);
            renderPass.AddRead(handle);
        }

        internal void AddWrite(RenderPassIndex renderPass, ResourceHandle handle)
        {
            ResourceVersion version = _entries.AsSpan()[handle.Index].Version!;
            Guard.IsNotNull(version);

            if (version.CurrentWriter != null)
            {
                renderPass.AddDependency(version.CurrentWriter);
            }

            ROList<RenderPassIndex> readers = version.Readers;
            for (int i = version.CurrentReadIndex; i < readers.Count; ++i)
            {
                renderPass.AddDependency(readers[i]);
            }

            version.Write(renderPass);
            renderPass.AddWrite(handle);
        }

        internal void AddReadWrite(RenderPassIndex renderPass, ResourceHandle handle)
        {
            ResourceVersion version = _entries.AsSpan()[handle.Index].Version!;
            Guard.IsNotNull(version);

            if (version.CurrentWriter != null)
            {
                renderPass.AddDependency(version.CurrentWriter);
            }

            ROList<RenderPassIndex> readers = version.Readers;
            for (int i = version.CurrentReadIndex; i < readers.Count; ++i)
            {
                renderPass.AddDependency(readers[i]);
            }

            version.Write(renderPass);

            renderPass.AddRead(handle);
            renderPass.AddWrite(handle);
            renderPass.AddReadWrite(handle);
        }

        private void ValidateBufferDesc(in FGBufferDesc bufferDesc)
        {
            bool shouldThrow = false;

            if (bufferDesc.ElementCount == 0)
            {
                EngLog.Render.Error("[{name}]: A buffer must have atleast one element", bufferDesc.Name);
                shouldThrow = true;
            }

            if (bufferDesc.Stride == 0)
            {
                EngLog.Render.Error("[{name}]: A buffer must have a byte stride of atleast one", bufferDesc.Name);
                shouldThrow = true;
            }

            if (bufferDesc.Target.HasFlags(FGBufferTarget.Constant))
            {
                if (bufferDesc.ElementCount * bufferDesc.Stride > 65535)
                {
                    EngLog.Render.Error("[{name}]: A buffer intended for use as a constant buffer must not be larger thant 65535 bytes", bufferDesc.Name);
                    shouldThrow = true;
                }
            }

            if (shouldThrow)
                throw new Exception($"Buffer '{bufferDesc.Name}' failed to validate for one or more reasons\n    {bufferDesc}");
        }

        private void ValidateTextureDesc(in FGTextureDesc textureDesc)
        {
            bool shouldThrow = false;

            if (textureDesc.Width <= 0 || textureDesc.Width > 16384)
            {
                EngLog.Render.Error("[{name}]: Texture width must be in the range of 1 - 16384", textureDesc.Name);
                shouldThrow = true;
            }

            if (textureDesc.Height <= 0 || textureDesc.Height > 16384)
            {
                EngLog.Render.Error("[{name}]: Texture width must be in the range of 1 - 16384", textureDesc.Height);
                shouldThrow = true;
            }

            if (textureDesc.DepthOrArraySize <= 0 || textureDesc.DepthOrArraySize > 2048)
            {
                if (textureDesc.Dimension == FGTextureDimension.Tex3D)
                    EngLog.Render.Error("[{name}]: Texture depth must be in range of 1 - 2048", textureDesc.Name);
                else if (textureDesc.Dimension == FGTextureDimension.Tex2DArray || textureDesc.Dimension == FGTextureDimension.CubeArray)
                    EngLog.Render.Error("[{name}]: Texture array size must be in range of 1 - 2048", textureDesc.Name);
                else
                    EngLog.Render.Error("[{name}]: Texture array size must be one since it is not an array", textureDesc.Name);
                shouldThrow = true;
            }

            int maxMipCount = Math.Max(textureDesc.Width, textureDesc.Height);
            if (textureDesc.Dimension == FGTextureDimension.Tex3D)
                maxMipCount = Math.Max(maxMipCount, textureDesc.DepthOrArraySize);

            maxMipCount = (int)MathF.Log2(maxMipCount) + 1;
            if (textureDesc.MipCount == 0 || textureDesc.MipCount > maxMipCount)
            {
                EngLog.Render.Error("[{name}]: Texture dimensions require mip count to be in the range 1 - {maxMips}", textureDesc.Name, maxMipCount);
                shouldThrow = true;
            }

            if (textureDesc.Format == RHIFormat.Unknown)
            {
                EngLog.Render.Error("[{name}]: Texture must have a known format", textureDesc.Name);
                shouldThrow = true;
            }

            if (textureDesc.Target.HasFlags(FGTextureTarget.RenderTarget))
            {
                if (!textureDesc.Format.IsRenderTargetCapable())
                {
                    EngLog.Render.Error("[{name}]: Texture for use as a render target has an incompatible format", textureDesc.Name);
                    shouldThrow = true;
                }
            }

            if (textureDesc.Target.HasFlags(FGTextureTarget.DepthStencil))
            {
                if (!textureDesc.Format.IsDepthStencilCapable())
                {
                    EngLog.Render.Error("[{name}]: Texture for use as a render target has an incompatible format", textureDesc.Name);
                    shouldThrow = true;
                }
            }

            if (shouldThrow)
                throw new Exception($"Texture '{textureDesc.Name}' failed to validate for one or more reasons\n    {textureDesc}");
        }

        private readonly record struct ImportedResourceData(int Index, RHIResource Resource, bool IsPure) : IEquatable<ImportedResourceData>;
    }

    internal readonly record struct GenericResourceDesc(string? Name, FGResourceType Type, FGResourceDesc Resource) : IEquatable<GenericResourceDesc>
    {
        public override int GetHashCode() => HashCode.Combine(Name, Resource);

        public static implicit operator GenericResourceDesc(in FGBufferDesc bufferDesc) => new GenericResourceDesc(bufferDesc.Name, FGResourceType.Buffer, new FGResourceDesc(bufferDesc));
        public static implicit operator GenericResourceDesc(in FGTextureDesc textureDesc) => new GenericResourceDesc(textureDesc.Name, FGResourceType.Texture, new FGResourceDesc(textureDesc));
    }

    [StructLayout(LayoutKind.Explicit)]
    internal record struct FGResourceDesc : IEquatable<FGResourceDesc>
    {
        [FieldOffset(0)]
        public Buffer BufferDesc;

        [FieldOffset(0)]
        public Texture TextureDesc;

        public FGResourceDesc(in FGBufferDesc desc)
        {
            BufferDesc = new Buffer
            {
                ElementCount = desc.ElementCount,
                Stride = desc.Stride,

                Target = desc.Target
            };
        }

        public FGResourceDesc(in FGTextureDesc desc)
        {
            TextureDesc = new Texture
            {
                Width = desc.Width,
                Height = desc.Height,
                DepthOrArraySize = desc.DepthOrArraySize,

                MipCount = desc.MipCount,

                Dimension = desc.Dimension,
                Format = desc.Format,
                Target = desc.Target
            };
        }

        public override int GetHashCode() => new Span<FGResourceDesc>(ref this).GetDjb2HashCode();

        internal record struct Buffer : IEquatable<Buffer>
        {
            public int ElementCount;
            public int Stride;

            public FGBufferTarget Target;
        }

        internal record struct Texture : IEquatable<Texture>
        {
            public int Width;
            public int Height;
            public int DepthOrArraySize;

            public int MipCount;

            public FGTextureDimension Dimension;
            public RHIFormat Format;
            public FGTextureTarget Target;
        }
    }

    public readonly record struct ResourceAlignmentInfo(int Alignment, int SizeInBytes);
}
