using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Raw
{
    //wrapper
    public sealed class CommandBuffer
    {
        private RHI.GraphicsCommandBuffer? _commandBuffer;

        private ShaderAsset? _activeShader;

        private readonly ShaderBindGroup?[] _bindGroups;
        private readonly RHI.ResourceLocation[] _locations;

        internal CommandBuffer()
        {
            _commandBuffer = null;
            _activeShader = null;

            _bindGroups = new ShaderBindGroup[8];
            _locations = new RHI.ResourceLocation[64];
        }

        internal void BindForUsage(RHI.GraphicsCommandBuffer commandBuffer)
        {
            _commandBuffer = commandBuffer;
            _activeShader = null;

            Array.Fill(_bindGroups, null);
            Array.Fill(_locations, new RHI.ResourceLocation(0, null, null, 0));
        }

        internal void UnbindUsage()
        {
            _commandBuffer = null;
            _activeShader = null;

            Array.Fill(_bindGroups, null);
            Array.Fill(_locations, new RHI.ResourceLocation(0, null, null, 0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nint Map(RHI.Buffer buffer, RHI.MapIntent intent, ulong writeSize = 0, ulong writeOffset = 0) => _commandBuffer!.Map(buffer, intent, writeSize, writeOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public nint Map(RHI.Texture texture, RHI.MapIntent intent, RHI.TextureLocation location, uint subresource = 0, uint rowPitch = 0) => _commandBuffer!.Map(texture, intent, location, subresource, rowPitch);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Unmap(RHI.Resource resource) => _commandBuffer!.Unmap(resource);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CopyBufferRegion(RHI.Buffer src, uint srcOffset, RHI.Buffer dst, uint dstOffset, uint size) => _commandBuffer!.CopyBufferRegion(src, srcOffset, dst, dstOffset, size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CopyTextureRegion(RHI.Resource src, RHI.TextureLocation srcLoc, uint srcSubRes, RHI.Resource dst, RHI.TextureLocation dstLoc, uint dstSubRes) => _commandBuffer!.CopyTextureRegion(src, srcLoc, srcSubRes, dst, dstLoc, dstSubRes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearRenderTarget(RHI.RenderTarget rt, Vector4 color) => _commandBuffer!.ClearRenderTarget(rt, color);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearDepthStencil(RHI.RenderTarget rt, RHI.ClearFlags clear, float depth = 1.0f, byte stencil = 0xff) => _commandBuffer!.ClearDepthStencil(rt, clear, depth, stencil);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRenderTarget(RHI.RenderTarget rt, bool alsoBindDepthStencil = false) => _commandBuffer!.SetRenderTargets(new Span<RHI.RenderTarget>(ref rt), alsoBindDepthStencil);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetRenderTargets(Span<RHI.RenderTarget> rts, bool setFirstAsDepthStencil = false) => _commandBuffer!.SetRenderTargets(rts, setFirstAsDepthStencil);
        public void SetDepthStencil(RHI.RenderTarget rt) => _commandBuffer!.SetDepthStencil(rt);

        public void SetViewport(RHI.Viewport viewport) => _commandBuffer!.SetViewports(new Span<RHI.Viewport>(ref viewport));
        public void SetViewports(Span<RHI.Viewport> viewports) => _commandBuffer!.SetViewports(viewports);
        public void SetScissorRect(RHI.ScissorRect scissor) => _commandBuffer!.SetScissorRects(new Span<RHI.ScissorRect>(ref scissor));
        public void SetScissorRects(Span<RHI.ScissorRect> scissors) => _commandBuffer!.SetScissorRects(scissors);

        public void SetStencilReference(uint stencilRef) => _commandBuffer!.SetStencilReference(stencilRef);

        public void SetVertexBuffer(int slot, RHI.Buffer buffer) => _commandBuffer!.SetVertexBuffers(slot, new Span<RHI.Buffer>(ref buffer), Span<uint>.Empty);
        public void SetVertexBuffers(int startSlot, Span<RHI.Buffer> buffers) => _commandBuffer!.SetVertexBuffers(startSlot, buffers, Span<uint>.Empty);
        public void SetIndexBuffer(RHI.Buffer? buffer) => _commandBuffer!.SetIndexBuffer(buffer);

        public void SetShader(ShaderAsset asset)
        {
            if (asset.GraphicsPipeline != null)
            {
                _activeShader = asset;
                _commandBuffer!.SetPipeline(_activeShader.GraphicsPipeline);
            }
        }

        //i think the JIT can actually remove bounds checking from the array setters because of predetermined size and constants magic numbers
        #region SetBindGroups overloads
        public void SetBindGroups(ShaderBindGroup sbg0)
        {
            _bindGroups[0] = sbg0;
            _bindGroups[1] = null;
        }

        public void SetBindGroups(ShaderBindGroup sbg0, ShaderBindGroup sbg1)
        {
            _bindGroups[0] = sbg0;
            _bindGroups[1] = sbg1;
            _bindGroups[2] = null;
        }

        public void SetBindGroups(ShaderBindGroup sbg0, ShaderBindGroup sbg1, ShaderBindGroup sbg2)
        {
            _bindGroups[0] = sbg0;
            _bindGroups[1] = sbg1;
            _bindGroups[2] = sbg2;
            _bindGroups[3] = null;
        }

        public void SetBindGroups(ShaderBindGroup sbg0, ShaderBindGroup sbg1, ShaderBindGroup sbg2, ShaderBindGroup sbg3)
        {
            _bindGroups[0] = sbg0;
            _bindGroups[1] = sbg1;
            _bindGroups[2] = sbg2;
            _bindGroups[3] = sbg3;
            _bindGroups[4] = null;
        }

        public void SetBindGroups(ShaderBindGroup sbg0, ShaderBindGroup sbg1, ShaderBindGroup sbg2, ShaderBindGroup sbg3, ShaderBindGroup sbg4)
        {
            _bindGroups[0] = sbg0;
            _bindGroups[1] = sbg1;
            _bindGroups[2] = sbg2;
            _bindGroups[3] = sbg3;
            _bindGroups[4] = sbg4;
            _bindGroups[5] = null;
        }

        public void SetBindGroups(ShaderBindGroup sbg0, ShaderBindGroup sbg1, ShaderBindGroup sbg2, ShaderBindGroup sbg3, ShaderBindGroup sbg4, ShaderBindGroup sbg5)
        {
            _bindGroups[0] = sbg0;
            _bindGroups[1] = sbg1;
            _bindGroups[2] = sbg2;
            _bindGroups[3] = sbg3;
            _bindGroups[4] = sbg4;
            _bindGroups[5] = sbg5;
            _bindGroups[6] = null;
        }

        public void SetBindGroups(ShaderBindGroup sbg0, ShaderBindGroup sbg1, ShaderBindGroup sbg2, ShaderBindGroup sbg3, ShaderBindGroup sbg4, ShaderBindGroup sbg5, ShaderBindGroup sbg6)
        {
            _bindGroups[0] = sbg0;
            _bindGroups[1] = sbg1;
            _bindGroups[2] = sbg2;
            _bindGroups[3] = sbg3;
            _bindGroups[4] = sbg4;
            _bindGroups[5] = sbg5;
            _bindGroups[6] = sbg6;
            _bindGroups[7] = null;
        }

        public void SetBindGroups(ShaderBindGroup sbg0, ShaderBindGroup sbg1, ShaderBindGroup sbg2, ShaderBindGroup sbg3, ShaderBindGroup sbg4, ShaderBindGroup sbg5, ShaderBindGroup sbg6, ShaderBindGroup sbg7)
        {
            _bindGroups[0] = sbg0;
            _bindGroups[1] = sbg1;
            _bindGroups[2] = sbg2;
            _bindGroups[3] = sbg3;
            _bindGroups[4] = sbg4;
            _bindGroups[5] = sbg5;
            _bindGroups[6] = sbg6;
            _bindGroups[7] = sbg7;
        }
        #endregion

        public void CommitShaderResources(ShaderAsset? shader = null)
        {
            if (_bindGroups[0] == null)
                return;

            shader ??= _activeShader;
            if (shader != null)
            {
                int sbgIndex = 0;
                int totalResourceCount = 0;

                do
                {
                    ShaderBindGroup bindGroup = _bindGroups[sbgIndex]!;
                    Debug.Assert(bindGroup != null);

                    int startIndex = shader.GetIndexForBindGroup(bindGroup.GroupName);
                    if (startIndex < 0)
                        continue; //fail silently! TODO: consider adding an alternative to ease debugging
                    //TODO: bounds checking!!

                    Span<BindGroupResourceLocation> staticResources = bindGroup.StaticResources;
                    Span<RHI.ResourceLocation> resourceLocations = _locations.AsSpan(startIndex, staticResources.Length);

                    for (int i = 0; i < staticResources.Length; i++)
                    {
                        ref BindGroupResourceLocation staticRes = ref staticResources[i];
                        if (staticRes.ConstantsOffset == ushort.MaxValue)
                            continue;

                        switch (staticRes.Type)
                        {
                            case BindGroupResourceType.TextureAsset:
                                {
                                    TextureAsset? asset = Unsafe.As<TextureAsset>(staticRes.Resource);
                                    if (asset == null || asset.Status != ResourceStatus.Success)
                                        resourceLocations[staticRes.ConstantsOffset] = new RHI.ResourceLocation((ushort)(startIndex + staticRes.ConstantsOffset), AssetManager.Static.DefaultWhite.RawRHITexture, null, 0);
                                    else
                                        resourceLocations[staticRes.ConstantsOffset] = new RHI.ResourceLocation((ushort)(startIndex + staticRes.ConstantsOffset), asset.RawRHITexture, null, 0);
                                    break;
                                }
                            case BindGroupResourceType.RHITexture:
                                {
                                    RHI.Texture? resource = Unsafe.As<RHI.Texture>(staticRes.Resource);
                                    resourceLocations[staticRes.ConstantsOffset] = new RHI.ResourceLocation((ushort)(startIndex + staticRes.ConstantsOffset), resource, null, 0);
                                    break;
                                }
                            case BindGroupResourceType.RHIBuffer:
                                {
                                    RHI.Buffer? resource = Unsafe.As<RHI.Buffer>(staticRes.Resource);
                                    resourceLocations[staticRes.ConstantsOffset] = new RHI.ResourceLocation((ushort)(startIndex + staticRes.ConstantsOffset), resource, null, 0);
                                    break;
                                }
                            case BindGroupResourceType.RHIRenderTextureView:
                                {
                                    RHI.RenderTextureView? resource = Unsafe.As<RHI.RenderTextureView>(staticRes.Resource);
                                    resourceLocations[staticRes.ConstantsOffset] = new RHI.ResourceLocation((ushort)(startIndex + staticRes.ConstantsOffset), resource, null, 0);
                                    break;
                                }
                            default: break;
                        }

                        totalResourceCount = Math.Max(startIndex + staticRes.ConstantsOffset + 1, totalResourceCount);
                    }
                } while (sbgIndex++ < _bindGroups.Length && _bindGroups[sbgIndex] != null);

                _commandBuffer!.SetResources(_locations.AsSpan(0, totalResourceCount));
            }
        }

        //TODO: check for boxing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetConstants<T>(T constants) where T : unmanaged
        {
            //TODO: add bounds checks for constants and appropriate T generic
            SetConstants(MemoryMarshal.Cast<T, uint>(new Span<T>(ref constants)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<T> Map<T>(RHI.Buffer buffer, RHI.MapIntent intent, int elementCount = 0, int elementOffset = 0) where T : unmanaged
        {
            if (elementCount == 0 && buffer.Description.Stride == 0)
                throw new ArgumentException("elementCount == 0 && buffer.Description.Stride == 0");

            if (elementCount == 0)
                elementCount = (int)(buffer.Description.ByteWidth / buffer.Description.Stride);

            uint size = (uint)Unsafe.SizeOf<T>();
            nint mapped = Map(buffer, intent, (ulong)(elementCount * size), (ulong)(elementOffset == 0 ? elementOffset : elementOffset * size));

            return mapped == nint.Zero ? Span<T>.Empty : new Span<T>(mapped.ToPointer(), elementCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearResources() => _commandBuffer!.SetResources(Span<RHI.ResourceLocation>.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetConstants(Span<uint> constants) => _commandBuffer!.SetConstants(constants);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearConstants() => _commandBuffer!.SetConstants(Span<uint>.Empty);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawIndexedInstanced(in RHI.DrawIndexedInstancedArgs args) => _commandBuffer!.DrawIndexedInstanced(args);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawInstanced(in RHI.DrawInstancedArgs args) => _commandBuffer!.DrawInstanced(args);

        public RHI.GraphicsCommandBuffer Wrapped => _commandBuffer!;
    }

    public record struct CommandBufferEventScope : IDisposable
    {
        private readonly RHI.CommandBuffer _commandBuffer;

        public CommandBufferEventScope(RasterCommandBuffer commandBuffer, ReadOnlySpan<char> name)
        {
            _commandBuffer = commandBuffer.Wrapped;
            _commandBuffer.BeginEvent(new Color32((uint)name.GetDjb2HashCode() | 0x000000ffu), name);
        }

        public void Dispose()
        {
            _commandBuffer.EndEvent();
        }
    }
}
