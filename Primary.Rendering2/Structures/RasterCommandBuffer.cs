using Primary.Common;
using Primary.Rendering;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Memory;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using Primary.RHI;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering2.Structures
{
    public ref struct RasterCommandBuffer
    {
        private readonly RenderPassErrorReporter _errorReporter;
        private readonly RenderPassStateData _stateData;
        private readonly CommandRecorder _recorder;
        private readonly SequentialLinearAllocator _intermediateAllocator;
        private readonly FrameGraphResources _resources;

        internal RasterCommandBuffer(RenderPassErrorReporter errorReporter, RenderPassStateData stateData, CommandRecorder recorder, SequentialLinearAllocator intermediateAllocator, FrameGraphResources resources)
        {
            _errorReporter = errorReporter;
            _stateData = stateData;
            _recorder = recorder;
            _intermediateAllocator = intermediateAllocator;
            _resources = resources;
        }

        public void SetRenderTarget(int slot, FrameGraphTexture renderTarget)
        {
            if (!renderTarget.IsExternal && !_stateData.ContainsOutput(renderTarget, FGRenderTargetType.RenderTarget))
            {
                _errorReporter.ReportError(RPErrorSource.SetRenderTarget, RPErrorType.InvalidOutput, renderTarget.ToString());
                return;
            }

            if ((uint)slot > 7)
            {
                _errorReporter.ReportError(RPErrorSource.SetRenderTarget, RPErrorType.SlotOutOfRange, renderTarget.ToString());
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetRenderTarget, RecCommandEffectFlags.ColorTarget, new UCSetRenderTarget
            {
                Slot = (byte)slot,
                RenderTarget = renderTarget.Index
            });
        }

        public void SetDepthStencil(FrameGraphTexture depthStencil)
        {
            if (!depthStencil.IsExternal && !_stateData.ContainsOutput(depthStencil, FGRenderTargetType.DepthStencil))
            {
                _errorReporter.ReportError(RPErrorSource.SetRenderTarget, RPErrorType.InvalidOutput, depthStencil.ToString());
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetDepthStencil, RecCommandEffectFlags.DepthStencilTarget, new UCSetDepthStencil
            {
                DepthStencil = depthStencil.Index
            });
        }

        public void ClearRenderTarget(FrameGraphTexture renderTarget, FGRect? rect = null)
        {
            if (!renderTarget.IsExternal && !_stateData.ContainsOutput(renderTarget, FGRenderTargetType.RenderTarget))
            {
                _errorReporter.ReportError(RPErrorSource.ClearRenderTarget, RPErrorType.InvalidOutput, renderTarget.ToString());
                return;
            }

            if (rect.HasValue)
            {
                FGRect val = rect.Value;

                if (val.Width <= 0 || val.Height <= 0)
                    return;
                if (!FGRect.Intersects(new FGRect(0, 0, renderTarget.Description.Width, renderTarget.Description.Height), val))
                    return;
            }

            _recorder.AddModificationCommand(RecCommandType.ClearRenderTarget, new UCClearRenderTarget
            {
                RenderTarget = renderTarget.Index,
                Rect = rect
            });
        }

        public void ClearDepthStencil(FrameGraphTexture depthStencil, FGClearFlags clearFlags, FGRect? rect = null)
        {
            if (!depthStencil.IsExternal && !_stateData.ContainsOutput(depthStencil, FGRenderTargetType.DepthStencil))
            {
                _errorReporter.ReportError(RPErrorSource.ClearDepthStencil, RPErrorType.InvalidOutput, depthStencil.ToString());
                return;
            }

            if (rect.HasValue)
            {
                FGRect val = rect.Value;

                if (val.Width <= 0 || val.Height <= 0)
                    return;
                if (!FGRect.Intersects(new FGRect(0, 0, depthStencil.Description.Width, depthStencil.Description.Height), val))
                    return;
            }

            _recorder.AddModificationCommand(RecCommandType.ClearDepthStencil, new UCClearDepthStencil
            {
                DepthStencil = depthStencil.Index,
                ClearFlags = clearFlags,
                Rect = rect
            });
        }

        public void ClearRenderTarget(FrameGraphTexture renderTarget, Color color, FGRect? rect = null)
        {
            if (!renderTarget.IsExternal && !_stateData.ContainsOutput(renderTarget, FGRenderTargetType.RenderTarget))
            {
                _errorReporter.ReportError(RPErrorSource.ClearRenderTarget, RPErrorType.InvalidOutput, renderTarget.ToString());
                return;
            }

            if (rect.HasValue)
            {
                FGRect val = rect.Value;

                if (val.Width <= 0 || val.Height <= 0)
                    return;
                if (!FGRect.Intersects(new FGRect(0, 0, renderTarget.Description.Width, renderTarget.Description.Height), val))
                    return;
            }

            _recorder.AddModificationCommand(RecCommandType.ClearRenderTargetCustom, new UCClearRenderTargetCustom
            {
                RenderTarget = renderTarget.Index,
                Rect = rect,
                Color = color
            });
        }

        public void ClearDepthStencil(FrameGraphTexture depthStencil, FGClearFlags clearFlags, float depth = 1.0f, byte stencil = 0xff, FGRect? rect = null)
        {
            if (!depthStencil.IsExternal && !_stateData.ContainsOutput(depthStencil, FGRenderTargetType.DepthStencil))
            {
                _errorReporter.ReportError(RPErrorSource.ClearDepthStencil, RPErrorType.InvalidOutput, depthStencil.ToString());
                return;
            }

            if (rect.HasValue)
            {
                FGRect val = rect.Value;

                if (val.Width <= 0 || val.Height <= 0)
                    return;
                if (!FGRect.Intersects(new FGRect(0, 0, depthStencil.Description.Width, depthStencil.Description.Height), val))
                    return;
            }

            _recorder.AddModificationCommand(RecCommandType.ClearDepthStencilCustom, new UCClearDepthStencilCustom
            {
                DepthStencil = depthStencil.Index,
                ClearFlags = clearFlags,
                Rect = rect,
                Depth = depth,
                Stencil = stencil
            });
        }

        public void SetViewport(int slot, FGViewport? viewport)
        {
            _recorder.AddStateChangeCommand(RecCommandType.SetViewport, RecCommandEffectFlags.Viewport, new UCSetViewport
            {
                Slot = slot,
                Viewport = viewport
            });
        }

        public void SetScissor(int slot, FGRect? scissor)
        {
            _recorder.AddStateChangeCommand(RecCommandType.SetScissor, RecCommandEffectFlags.Scissor, new UCSetScissor
            {
                Slot = slot,
                Scissor = scissor
            });
        }

        public void SetStencilReference(uint stencilRef)
        {
            _recorder.AddStateChangeCommand(RecCommandType.SetStencilReference, RecCommandEffectFlags.StencilRef, new UCSetStencilRef
            {
                StencilRef = stencilRef
            });
        }

        public void SetVertexBuffer(FGSetBufferDesc desc)
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.SetVertexBuffer, RPErrorType.NoShaderAccess, desc.Buffer.ToString());
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetBuffer, RecCommandEffectFlags.VertexBuffer, new UCSetBuffer
            {
                IsExternal = desc.Buffer.IsExternal,
                Buffer = desc.Buffer.IsExternal ? desc.Buffer.Resource!.Handle : desc.Buffer.Index,
                Location = FGSetBufferLocation.VertexBuffer,
                Stride = desc.Stride
            });
        }

        public void SetIndexBuffer(FGSetBufferDesc desc)
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.SetIndexBuffer, RPErrorType.NoShaderAccess, desc.Buffer.ToString());
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetBuffer, RecCommandEffectFlags.IndexBuffer, new UCSetBuffer
            {
                IsExternal = desc.Buffer.IsExternal,
                Buffer = desc.Buffer.IsExternal ? desc.Buffer.Resource!.Handle : desc.Buffer.Index,
                Location = FGSetBufferLocation.IndexBuffer,
                Stride = desc.Stride
            });
        }

        public void SetPipeline(GraphicsPipeline pipeline)
        {
            _recorder.AddStateChangeCommand(RecCommandType.SetPipeline, RecCommandEffectFlags.Pipeline, new UCSetPipeline
            {
                Pipeline = pipeline.Handle
            });
        }

        public void SetProperties(PropertyBlock block)
        {
            if (block.IsOutOfDate)
                block.Reload();

            _recorder.AddSetParameters(block);
        }

        public void SetProperties(ROPropertyBlock block)
        {
            if (!block.IsNull)
                _recorder.AddSetParameters(block.InternalBlock!);
        }

        public void DrawInstanced(FGDrawInstancedDesc desc)
        {
            _recorder.AddExecutionCommand(RecCommandType.DrawInstanced, new UCDrawInstanced
            {
                VertexCount = desc.VertexCountPerInstance,
                InstanceCount = desc.InstanceCount,
                StartVertex = desc.StartVertexLocation,
                StartInstance = desc.StartInstanceLocation
            });
        }

        public void DrawIndexedInstanced(FGDrawIndexedInstancedDesc desc)
        {
            _recorder.AddExecutionCommand(RecCommandType.DrawIndexedInstanced, new UCDrawIndexedInstanced
            {
                IndexCount = desc.IndexCountPerInstance,
                InstanceCount = desc.InstanceCount,
                StartIndex = desc.StartIndexLocation,
                BaseVertex = desc.BaseVertexLocation,
                StartInstance = desc.StartInstanceLocation
            });
        }

        public void Upload<T>(FGBufferUploadDesc desc, Span<T> data) where T : unmanaged
        {

        }

        public unsafe void Upload<T>(FrameGraphBuffer buffer, T data) where T : unmanaged
        {
            if (!buffer.IsExternal && !_stateData.ContainsResource(buffer, FGResourceUsage.Write))
            {
                _errorReporter.ReportError(RPErrorSource.UploadBuffer, RPErrorType.NoResourceAccess, buffer.ToString());
                return;
            }

            if (buffer.Description.Width < Unsafe.SizeOf<T>())
            {
                _errorReporter.ReportError(RPErrorSource.UploadBuffer, RPErrorType.ResourceTooSmall, buffer.ToString());
                return;
            }

            nint ptr = _intermediateAllocator.Allocate(Unsafe.SizeOf<T>());
            *(T*)ptr.ToPointer() = data;

            int index = _resources.AddBufferUpload(buffer, 0, Unsafe.SizeOf<T>());

            _recorder.AddModificationCommand(RecCommandType.UploadBuffer, new UCUploadBuffer
            {
                BufferUploadIndex = index,
                DataPointer = ptr,
                DataSize = (uint)Unsafe.SizeOf<T>(),
                BufferOffset = 0
            });
        }

        public void Upload<T>(FGTextureUploadDesc desc, Span<T> data) where T : unmanaged
        {
           
        }

        public void Copy<T>(FGBufferCopyDesc desc) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void Copy<T>(FGTextureCopyDesc desc) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public unsafe FGMappedSubresource<T> Map<T>(FGMapBufferDesc desc) where T : unmanaged
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Write))
            {
                _errorReporter.ReportError(RPErrorSource.MapBuffer, RPErrorType.NoResourceAccess, desc.Buffer.ToString());
                return new FGMappedSubresource<T>(this, desc, nint.Zero, Span<T>.Empty);
            }

            uint actualBufferSize = desc.Buffer.Description.Width;
            uint totalElementCount = (uint)(desc.ElementCount == 0 ? (actualBufferSize / Unsafe.SizeOf<T>()) : desc.ElementCount);
            uint totalElementSize = (uint)(totalElementCount * Unsafe.SizeOf<T>());

            if ((desc.ElementCount == 0 ? actualBufferSize : desc.ElementCount) + desc.Offset > actualBufferSize)
            {
                _errorReporter.ReportError(RPErrorSource.MapBuffer, RPErrorType.OutOfRange, desc.Buffer.ToString());
                return new FGMappedSubresource<T>(this, desc, nint.Zero, Span<T>.Empty);
            }

            nint ptr = _intermediateAllocator.Allocate((int)totalElementSize);
            return new FGMappedSubresource<T>(this, desc, ptr, new Span<T>(ptr.ToPointer(), (int)totalElementCount));
        }

        public unsafe FGMappedSubresource<T> Map<T>(FrameGraphTexture texture) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        internal void Unmap(nint rawPtr, uint dataSize, FGMapBufferDesc desc)
        {
            int index = _resources.AddBufferUpload(desc.Buffer, (int)desc.Offset, (int)dataSize);

            _recorder.AddModificationCommand(RecCommandType.UploadBuffer, new UCUploadBuffer
            {
                BufferUploadIndex = index,
                DataPointer = rawPtr,
                DataSize = dataSize,
                BufferOffset = desc.Offset
            });
        }

        internal void PresentOnWindow(Window window, FrameGraphTexture texture)
        {
            if (!_stateData.ContainsResource(texture, FGResourceUsage.Read | FGResourceUsage.NoShaderAccess))
            {
                _errorReporter.ReportError(RPErrorSource.PresentOnWindow, RPErrorType.NoResourceAccess, texture.ToString());
                return;
            }

            _recorder.AddExecutionCommand(RecCommandType.PresentOnWindow, new UCPresentOnWindow
            {
                IsExternal = texture.IsExternal,
                Texture = texture.IsExternal ? texture.Resource!.Handle : texture.Index,
                WindowId = window.WindowId
            });
        }

        #region Barriers
        public void Barrier(FrameGraphBuffer buffer, FGBarrierSync syncAfter, FGBarrierAccess accessAfter, ulong offset, ulong size)
        {

        }

        public void Barrier(FrameGraphTexture texture, FGBarrierSync syncAfter, FGBarrierAccess accessAfter, FGBarrierLayout layoutAfter, FGBarrierSubresource subresource)
        {

        }
        #endregion
    }

    public readonly record struct FGSetBufferDesc(FrameGraphBuffer Buffer, int Stride = 0)
    {
        public static implicit operator FGSetBufferDesc(FrameGraphBuffer buffer) => new FGSetBufferDesc(buffer);
    }

    public readonly record struct FGDrawInstancedDesc(uint VertexCountPerInstance, uint InstanceCount = 1, uint StartVertexLocation = 0, uint StartInstanceLocation = 0);
    public readonly record struct FGDrawIndexedInstancedDesc(uint IndexCountPerInstance, uint InstanceCount = 1, uint StartIndexLocation = 0, int BaseVertexLocation = 0, uint StartInstanceLocation = 0);

    public readonly record struct FGBufferUploadDesc(FrameGraphBuffer Buffer, uint Offset)
    {
        public static implicit operator FGBufferUploadDesc(FrameGraphBuffer buffer) => new FGBufferUploadDesc(buffer, 0);
    }

    public readonly record struct FGTextureUploadDesc(FrameGraphTexture Texture, FGBox? DestinationBox, int SubresourceIndex)
    {
        public static implicit operator FGTextureUploadDesc(FrameGraphTexture texture) => new FGTextureUploadDesc(texture, null, 0);
    }

    public readonly record struct FGBufferCopyDesc(FrameGraphBuffer Source, uint SrcOffset, FrameGraphBuffer Destination, uint DstOffset, uint NumBytes);
    public readonly record struct FGTextureCopyDesc(FGTextureCopySource Source, FGBox? SourceBox, FGTextureCopySource Destination, uint DstX, uint DstY, uint DstZ);

    public readonly record struct FGBarrierSubresource(int IndexOrFirstMipLevel, int NumMipLevels, int FirstArraySlice, int NumArraySlices, int FirstPlane, int NumPlanes);

    public readonly record struct FGTextureCopySource
    {
        public FrameGraphResource Resource { get; init; }
        public FGTextureCopySourceType Type { get; init; }

        public uint SubresourceIndex => _union.SubresourceIndex;
        public FGTextureFootprint Footprint => _union.Footprint;

        private __Union _union { get; init; }

        public FGTextureCopySource(FrameGraphTexture Resource, FGTextureCopySourceType Type, uint SubresourceIndex)
        {
            this.Resource = Resource;
            this.Type = Type;
            _union = new __Union(SubresourceIndex);
        }

        public FGTextureCopySource(FrameGraphResource Resource, FGTextureCopySourceType Type, FGTextureFootprint Footprint)
        {
            this.Resource = Resource;
            this.Type = Type;
            _union = new __Union(Footprint);
        }

        [StructLayout(LayoutKind.Explicit)]
        private readonly record struct __Union
        {
            [FieldOffset(0)]
            public readonly uint SubresourceIndex;
            [FieldOffset(0)]
            public readonly FGTextureFootprint Footprint;

            public __Union(uint subresourceIndex) => SubresourceIndex = subresourceIndex;
            public __Union(FGTextureFootprint footprint) => Footprint = footprint;
        }
    }

    public readonly record struct FGTextureFootprint(uint Offset, FGTextureFormat Format, uint Width, uint Height, uint Depth);

    public readonly record struct FGMapBufferDesc(FrameGraphBuffer Buffer, uint Offset, uint ElementCount)
    {
        public static implicit operator FGMapBufferDesc(FrameGraphBuffer buffer) => new FGMapBufferDesc(buffer, 0, 0);
    }

    public readonly record struct FGMapTextureDesc(FrameGraphTexture Texture, FGBox? Box)
    {
        public static implicit operator FGMapTextureDesc(FrameGraphTexture texture) => new FGMapTextureDesc(texture, null);
    }

    public readonly ref struct FGMappedSubresource<T> : IDisposable where T : unmanaged
    {
        private readonly RasterCommandBuffer _commandBuffer;
        private readonly FGMapBufferDesc _desc;

        private readonly nint _raw;
        private readonly Span<T> _data;

        internal FGMappedSubresource(RasterCommandBuffer commandBuffer, FGMapBufferDesc desc, nint raw, Span<T> data)
        {
            _commandBuffer = commandBuffer;
            _desc = desc;
            _raw = raw;
            _data = data;
        }

        public void Dispose()
        {
            if (_raw != nint.Zero)
                _commandBuffer.Unmap(_raw, (uint)(_data.Length * Unsafe.SizeOf<T>()), _desc);
        }

        public Span<T> Span => _data;
    }

    public enum FGClearFlags : byte
    {
        Depth = 1 << 0,
        Stencil = 1 << 1,

        DepthStencil = Depth | Stencil
    }

    public enum FGBarrierSync : byte
    {
        None = 0,
        All,
        Draw,
        IndexInput,
        VertexShading,
        PixelShading,
        DepthStencil,
        RenderTarget,
        ComputeShading,
        Raytracing,
        Copy,
        Resolve,
        ExecuteIndirect,
        Predication,
        AllShading,
        NonPixelShading,
        ClearUnorderedAccessView,
        Split
    }

    public enum FGBarrierAccess : byte
    {
        Common = 0,
        VertexBuffer,
        ConstantBuffer,
        IndexBuffer,
        RenderTarget,
        UnorderedAccess,
        DepthStencilWrite,
        DepthStencilRead,
        ShaderResource,
        StreamOutput,
        IndirectArgument,
        Predication,
        CopyDestination,
        CopySource,
        ResolveSource,
        ResolveDestination,
        ShadingRateSource,
        NoAccess
    }

    public enum FGBarrierLayout : byte
    {
        Undefined,
        Common,
        Present,
        GenericRead,
        RenderTarget,
        UnorderedAccess,
        DepthStencilWrite,
        DepthStencilRead,
        ShaderResource,
        CopySource,
        CopyDestination,
        ResolveSource,
        ResolveDestination,
        ShadingRateSource,
    }

    public enum FGTextureCopySourceType : byte
    {
        SubresourceIndex = 0,
        Footprint
    }

    public enum FGSetBufferLocation : byte
    {
        VertexBuffer,
        IndexBuffer,
    }
}
