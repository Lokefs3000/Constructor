using Arch.Core;
using Primary.Common;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using Primary.RHI;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Primary.Rendering2.Structures
{
    public ref struct RasterCommandBuffer
    {
        private readonly RenderPassErrorReporter _errorReporter;
        private readonly RenderPassStateData _stateData;
        private readonly CommandRecorder _recorder;

        internal RasterCommandBuffer(RenderPassErrorReporter errorReporter, RenderPassStateData stateData, CommandRecorder recorder)
        {
            _errorReporter = errorReporter;
            _stateData = stateData;
            _recorder = recorder;
        }

        public void SetRenderTarget(int slot, FrameGraphTexture renderTarget)
        {
            if (!_stateData.ContainsOutput(renderTarget, FGOutputType.RenderTarget))
            {
                _errorReporter.ReportError(RPErrorSource.SetRenderTarget, RPErrorType.InvalidOutput);
                return;
            }

            if ((uint)slot > 7)
            {
                _errorReporter.ReportError(RPErrorSource.SetRenderTarget, RPErrorType.SlotOutOfRange);
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
            if (!_stateData.ContainsOutput(depthStencil, FGOutputType.DepthStencil))
            {
                _errorReporter.ReportError(RPErrorSource.SetRenderTarget, RPErrorType.InvalidOutput);
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetDepthStencil, RecCommandEffectFlags.DepthStencilTarget, new UCSetDepthStencil
            {
                DepthStencil = depthStencil.Index
            });
        }

        public void ClearRenderTarget(FrameGraphTexture renderTarget, FGRect? rect = null)
        {
            if (!_stateData.ContainsOutput(renderTarget, FGOutputType.RenderTarget))
            {
                _errorReporter.ReportError(RPErrorSource.ClearRenderTarget, RPErrorType.InvalidOutput);
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
            if (!_stateData.ContainsOutput(depthStencil, FGOutputType.DepthStencil))
            {
                _errorReporter.ReportError(RPErrorSource.ClearDepthStencil, RPErrorType.InvalidOutput);
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
            if (!_stateData.ContainsOutput(renderTarget, FGOutputType.RenderTarget))
            {
                _errorReporter.ReportError(RPErrorSource.ClearRenderTarget, RPErrorType.InvalidOutput);
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
            if (!_stateData.ContainsOutput(depthStencil, FGOutputType.DepthStencil))
            {
                _errorReporter.ReportError(RPErrorSource.ClearDepthStencil, RPErrorType.InvalidOutput);
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
            _recorder.AddStateChangeCommand(RecCommandType.SetViewports, RecCommandEffectFlags.Viewport, new UCSetViewports
            {
                Slot = slot,
                Viewport = viewport
            });
        }

        public void SetScissor(int slot, FGRect? scissor)
        {
            _recorder.AddStateChangeCommand(RecCommandType.SetScissors, RecCommandEffectFlags.Scissor, new UCSetScissor
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
            if (!_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.SetVertexBuffer, RPErrorType.NoShaderAccess);
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetBuffer, RecCommandEffectFlags.VertexBuffer, new UCSetBuffer
            {
                Buffer = desc.Buffer.Index,
                Location = FGSetBufferLocation.VertexBuffer,
                Stride = desc.Stride
            });
        }

        public void SetIndexBuffer(FGSetBufferDesc desc)
        {
            if (!_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.SetIndexBuffer, RPErrorType.NoShaderAccess);
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetBuffer, RecCommandEffectFlags.IndexBuffer, new UCSetBuffer
            {
                Buffer = desc.Buffer.Index,
                Location = FGSetBufferLocation.IndexBuffer,
                Stride = desc.Stride
            });
        }

        public void SetProperties(PropertyBlock block)
        {

        }

        public void Upload<T>(FGBufferUploadDesc desc, Span<T> data) where T : unmanaged
        {

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

        public FGMappedSubresource<T> Map<T>(FGMapBufferDesc desc)
        {
            throw new NotImplementedException();
        }

        public FGMappedSubresource<T> Map<T>(FrameGraphTexture texture)
        {
            throw new NotImplementedException();
        }

        internal void Unmap(int mapId)
        {
            throw new NotImplementedException();
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

    public record struct FGSetBufferDesc(FrameGraphBuffer Buffer, int Stride = 0)
    {
        public static implicit operator FGSetBufferDesc(FrameGraphBuffer buffer) => new FGSetBufferDesc(buffer);
    }

    public record struct FGBufferUploadDesc(FrameGraphBuffer Buffer, uint Offset)
    {
        public static implicit operator FGBufferUploadDesc(FrameGraphBuffer buffer) => new FGBufferUploadDesc(buffer, 0);
    }

    public record struct FGTextureUploadDesc(FrameGraphTexture Texture, FGBox? DestinationBox, int SubresourceIndex)
    {
        public static implicit operator FGTextureUploadDesc(FrameGraphTexture texture) => new FGTextureUploadDesc(texture, null, 0);
    }

    public record struct FGBufferCopyDesc(FrameGraphBuffer Source, uint SrcOffset, FrameGraphBuffer Destination, uint DstOffset, uint NumBytes);
    public record struct FGTextureCopyDesc(FGTextureCopySource Source, FGBox? SourceBox, FGTextureCopySource Destination, uint DstX, uint DstY, uint DstZ);

    public record struct FGBarrierSubresource(int IndexOrFirstMipLevel, int NumMipLevels, int FirstArraySlice, int NumArraySlices, int FirstPlane, int NumPlanes);

    public record struct FGTextureCopySource
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

    public record struct FGTextureFootprint(uint Offset, FGTextureFormat Format, uint Width, uint Height, uint Depth);

    public record struct FGMapBufferDesc(FrameGraphBuffer Buffer, uint Offset, uint ElementCount)
    {
        public static implicit operator FGMapBufferDesc(FrameGraphBuffer buffer) => new FGMapBufferDesc(buffer, 0, 0);
    }

    public ref struct FGMappedSubresource<T> : IDisposable
    {
        private RasterCommandBuffer _commandBuffer;
        private int _mapId;

        private nint _raw;
        private Span<T> _data;

        internal FGMappedSubresource(RasterCommandBuffer commandBuffer, int mapId, nint raw, Span<T> data)
        {
            _commandBuffer = commandBuffer;
            _mapId = mapId;
            _raw = raw;
            _data = data;
        }

        public void Dispose()
        {
            if (_mapId != -1)
                _commandBuffer.Unmap(_mapId);
            _mapId = -1;

            _raw = nint.Zero;
            _data = Span<T>.Empty;
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
