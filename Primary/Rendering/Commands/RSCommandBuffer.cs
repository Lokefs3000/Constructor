using Primary.Common;
using Primary.Common.Memory;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.State;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Commands
{
    internal sealed class RSCommandBuffer : CommandBuffer
    {
        public RSCommandBuffer(RenderPassErrorReporter errorReporter, RenderPassStateData stateData, CommandRecorder recorder, LinearBlockAllocator intermediateAllocator, FrameGraphResources resources, RenderState state) : base(errorReporter, stateData, recorder, intermediateAllocator, resources, state)
        {
        }

        public unsafe void SetRenderTarget(int slot, FrameGraphTexture renderTarget)
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

            Raster.SetRenderTarget(slot, renderTarget);
        }

        public void SetDepthStencil(FrameGraphTexture depthStencil)
        {
            if (!depthStencil.IsExternal && !_stateData.ContainsOutput(depthStencil, FGRenderTargetType.DepthStencil))
            {
                _errorReporter.ReportError(RPErrorSource.SetRenderTarget, RPErrorType.InvalidOutput, depthStencil.ToString());
                return;
            }

            Raster.SetDepthStencil(depthStencil);
        }

        public void ClearRenderTarget(FrameGraphTexture renderTarget, Color? color, FGRect? rect = null)
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

            _recorder.AddCommand(RecCommandType.ClearRenderTarget, new CmdClearRenderTarget
            {
                Texture = renderTarget,
                Color = color,
                Rect = rect
            });
        }

        public void ClearDepthStencil(FrameGraphTexture depthStencil, FGClearFlags clearFlags, float? depth, byte? stencil, FGRect? rect = null)
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

            _recorder.AddCommand(RecCommandType.ClearDepthStencil, new CmdClearDepthStencil
            {
                Texture = depthStencil,
                ClearFlags = clearFlags,
                Depth = depth,
                Stencil = stencil,
                Rect = rect
            });
        }

        public void SetViewport(int slot, FGViewport? viewport)
        {
            Raster.SetViewport(slot, viewport);
        }

        public void SetScissor(int slot, FGRect? scissor)
        {
            Raster.SetScissor(slot, scissor);
        }

        public void SetStencilReference(uint stencilRef)
        {
            Raster.SetStencilRef(stencilRef);
        }

        public unsafe void SetVertexBuffer(FGSetBufferDesc desc)
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.SetVertexBuffer, RPErrorType.NoShaderAccess, desc.Buffer.ToString());
                return;
            }

            Raster.SetVertexBuffer(new SetVertexBufferData(desc.Buffer, (uint)desc.Stride));
        }

        public unsafe void SetIndexBuffer(FGSetBufferDesc desc)
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.SetIndexBuffer, RPErrorType.NoShaderAccess, desc.Buffer.ToString());
                return;
            }

            Raster.SetIndexBuffer(new SetIndexBufferData(desc.Buffer, (uint)desc.Stride));
        }

        public void SetPipeline(RHIGraphicsPipeline pipeline)
        {
            int index = _resources.AddPotentialPipeline(pipeline);

            Raster.SetPipeline(index);
            Raster.SetPipelineLimits(pipeline);
        }

        public void DrawInstanced(FGDrawInstancedDesc desc)
        {
            if (Raster.CommitState(_intermediateAllocator, _recorder))
            {
                _recorder.AddCommand(RecCommandType.DrawInstanced, new CmdDrawInstanced
                {
                    VertexCount = desc.VertexCountPerInstance,
                    InstanceCount = desc.InstanceCount,
                    StartVertex = desc.StartVertexLocation,
                    StartInstance = desc.StartInstanceLocation
                });
            }
        }

        public void DrawIndexedInstanced(FGDrawIndexedInstancedDesc desc)
        {
            if (Raster.CommitState(_intermediateAllocator, _recorder))
            {
                _recorder.AddCommand(RecCommandType.DrawIndexedInstanced, new CmdDrawIndexedInstanced
                {
                    IndexCount = desc.IndexCountPerInstance,
                    InstanceCount = desc.InstanceCount,
                    StartIndex = desc.StartIndexLocation,
                    BaseVertex = desc.BaseVertexLocation,
                    StartInstance = desc.StartInstanceLocation
                });
            }
        }

        internal unsafe void PresentOnWindow(Window window, FrameGraphTexture texture)
        {
            if (!_stateData.ContainsResource(texture, FGResourceUsage.Read | FGResourceUsage.NoShaderAccess))
            {
                _errorReporter.ReportError(RPErrorSource.PresentOnWindow, RPErrorType.NoResourceAccess, texture.ToString());
                return;
            }

            _recorder.AddCommand(RecCommandType.PresentOnWindow, new CmdPresentOnWindow
            {
                Texture = texture,
                WindowId = window.WindowId
            });

            _recorder.AddResourceToSet(texture);
        }

        private RasterState Raster => Unsafe.As<RasterState>(_state);
    }

    public readonly record struct FGSetBufferDesc(FrameGraphBuffer Buffer, int Stride = 0)
    {
        public static implicit operator FGSetBufferDesc(FrameGraphBuffer buffer) => new FGSetBufferDesc(buffer);
    }

    public readonly record struct FGDrawInstancedDesc(uint VertexCountPerInstance, uint InstanceCount = 1, uint StartVertexLocation = 0, uint StartInstanceLocation = 0);
    public readonly record struct FGDrawIndexedInstancedDesc(uint IndexCountPerInstance, uint InstanceCount = 1, uint StartIndexLocation = 0, int BaseVertexLocation = 0, uint StartInstanceLocation = 0);

    public readonly record struct FGBarrierSubresource(int IndexOrFirstMipLevel, int NumMipLevels, int FirstArraySlice, int NumArraySlices, int FirstPlane, int NumPlanes);

    public enum FGClearFlags : byte
    {
        Depth = 1 << 0,
        Stencil = 1 << 1,

        DepthStencil = Depth | Stencil
    }

    public enum FGSetBufferLocation : byte
    {
        VertexBuffer,
        IndexBuffer,
    }
}
