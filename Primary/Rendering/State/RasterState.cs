using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Commands;
using Primary.Rendering.Datatypes;
using Primary.Rendering.Recording;
using Primary.Rendering.Statistics;
using Primary.RHI;

namespace Primary.Rendering.State
{
    internal sealed class RasterState : RenderState
    {
        private readonly FrameGraph _frameGraph;

        private DirtyArrayNullable<TextureHandle> _renderTargets;
        private DirtyValueNullable<TextureHandle> _depthStencil;

        private DirtyArrayNullable<FGViewport> _viewports;
        private DirtyArrayNullable<FGRect> _scissors;

        private DirtyValue<uint> _stencilRef;
        private DirtyValue<Color> _blendFactor;

        private DirtyValueNullable<SetVertexBufferData> _vertexBuffer;
        private DirtyValueNullable<SetIndexBufferData> _indexBuffer;

        private DirtyValue<int> _shader;

        private IndexRange _renderTargetsRange;

        internal RasterState(FrameGraph frameGraph) : base(frameGraph)
        {
            _frameGraph = frameGraph;

            _renderTargets = new DirtyArrayNullable<TextureHandle>(8, TextureHandle.Null);
            _depthStencil = new DirtyValueNullable<TextureHandle>(TextureHandle.Null);

            _viewports = new DirtyArrayNullable<FGViewport>(8, null);
            _scissors = new DirtyArrayNullable<FGRect>(8, null);

            _stencilRef = new DirtyValue<uint>(0);
            _blendFactor = new DirtyValue<Color>(Color.White);

            _vertexBuffer = new DirtyValueNullable<SetVertexBufferData>(null);
            _indexBuffer = new DirtyValueNullable<SetIndexBufferData>(null);

            _shader = new DirtyValue<int>(-1);

            _renderTargetsRange = IndexRange.Empty;
        }

        protected override void DisposeInternal(bool disposing) { }

        internal override void ClearState()
        {
            base.ClearState();

            _renderTargets.Fill(null, false);
            _depthStencil.Reset(null);

            _viewports.Fill(null, false);
            _scissors.Fill(null, false);

            _stencilRef.Reset(0);
            _blendFactor.Reset(Color.White);

            _vertexBuffer.Reset(null);
            _indexBuffer.Reset(null);

            _shader.Reset(-1);

            _renderTargetsRange = IndexRange.Empty;
        }

        internal override void SoftResetForNextPass()
        {
            base.SoftResetForNextPass();

            _renderTargets.FillNew(null);
            _depthStencil.Value = null;

            _viewports.FillNew(null);
            _scissors.FillNew(null);

            _stencilRef.Value = 0;
            _blendFactor.Value = Color.White;

            _vertexBuffer.Value = null;
            _indexBuffer.Value = null;

            _shader.Value = -1;

            _renderTargetsRange = IndexRange.Empty;
        }

        internal override bool TryCommitState(ref RenderPassStats stats, BarrierGroup barrierGroup, DescriptorAllocator descriptorAllocator, CommandRecorder commandRecorder, ObjectsCache objectsCache)
        {
            if (_shader.Value == -1)
                return false;

            RHIGraphicsPipeline shader = objectsCache.RetrieveObject<RHIGraphicsPipeline>(_shader.Value);

            bool renderTargetsNeedUpdate = false;

            if (_renderTargets.IsAnyDirty)
            {
                int count = _renderTargets.DirtyCount;

                _renderTargetsRange = new IndexRange(-1, -1);
                for (int i = 0; i < count; ++i)
                {
                    TextureHandle? renderTarget = _renderTargets[i];

                    if (_renderTargets.IsDirty(i))
                    {
                        RHIFormat format = RHIFormat.Unknown;
                        if (renderTarget.HasValue)
                        {
                            ref TextureHandle handle = ref renderTarget.DangerousGetValueOrDefaultReference();
                            format = handle.IsImported ?
                                _frameGraph.ResourceManager.GetRHIResource(handle).Description.Format :
                                _frameGraph.ResourceManager.GetDescription(handle).Format;
                        }

                        commandRecorder.RecordCommand(CmdCommandType.SetRenderTarget, new CmdSetRenderTarget
                        {
                            RenderTargetFormat = format,
                            RenderTarget = descriptorAllocator.GetDescriptorForRenderTarget(renderTarget.GetValueOrDefault(TextureHandle.Null)),
                            Slot = (byte)i
                        });

                        renderTargetsNeedUpdate = true;

                        if (renderTarget.HasValue)
                        {
                            ref TextureHandle handle = ref renderTarget.DangerousGetValueOrDefaultReference();
                            barrierGroup.RequestBarrier(handle, commandRecorder.CommandOffset, ResourceBarrierSync.RenderTarget, ResourceBarrierAccess.RenderTarget);

                            if (_viewports[i] == null)
                            {
                                if (handle.IsImported)
                                {
                                    ref readonly RHITextureDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(handle).Description;
                                    _viewports[i] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                                }
                                else
                                {
                                    ref readonly FGTextureDesc desc = ref _frameGraph.ResourceManager.GetDescription(handle);
                                    _viewports[i] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                                }
                            }

                            if (_scissors[i] == null)
                            {
                                if (handle.IsImported)
                                {
                                    ref readonly RHITextureDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(handle).Description;
                                    _scissors[i] = new FGRect(0, 0, desc.Width, desc.Height);
                                }
                                else
                                {
                                    ref readonly FGTextureDesc desc = ref _frameGraph.ResourceManager.GetDescription(handle);
                                    _scissors[i] = new FGRect(0, 0, desc.Width, desc.Height);
                                }
                            }

                            if (_renderTargetsRange.Start == -1)
                                _renderTargetsRange.Start = i;
                            _renderTargetsRange.End = i;
                        }
                    }
                }

                if (_renderTargetsRange.Start == -1 || _renderTargetsRange.End == -1)
                    _renderTargetsRange = IndexRange.Empty;
                else
                    ++_renderTargetsRange.End;

                _renderTargets.ClearDirty();
            }
            else if (_couldBarriersHaveChanged)
            {
                for (int i = _renderTargetsRange.Start; i < _renderTargetsRange.End; ++i)
                {
                    TextureHandle? renderTarget = _renderTargets[i];
                    if (renderTarget.HasValue)
                    {
                        barrierGroup.RequestBarrier(renderTarget.GetValueOrDefault(), commandRecorder.CommandOffset, ResourceBarrierSync.RenderTarget, ResourceBarrierAccess.RenderTarget);
                    }
                }
            }

            if (_depthStencil.IsDirty)
            {
                TextureHandle? depthStencil = _depthStencil.Value;
                if (depthStencil.HasValue)
                {
                    ref TextureHandle handle = ref depthStencil.DangerousGetValueOrDefaultReference();
                    barrierGroup.RequestBarrier(handle, commandRecorder.CommandOffset, ResourceBarrierSync.DepthStencil, ResourceBarrierAccess.DepthStencilWrite);

                    if (!_renderTargets[0].HasValue)
                    {
                        if (_viewports[0] == null)
                        {
                            if (handle.IsImported)
                            {
                                ref readonly RHITextureDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(handle).Description;
                                _viewports[0] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                            }
                            else
                            {
                                ref readonly FGTextureDesc desc = ref _frameGraph.ResourceManager.GetDescription(handle);
                                _viewports[0] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                            }
                        }

                        if (_scissors[0] == null)
                        {
                            if (handle.IsImported)
                            {
                                ref readonly RHITextureDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(handle).Description;
                                _scissors[0] = new FGRect(0, 0, desc.Width, desc.Height);
                            }
                            else
                            {
                                ref readonly FGTextureDesc desc = ref _frameGraph.ResourceManager.GetDescription(handle);
                                _scissors[0] = new FGRect(0, 0, desc.Width, desc.Height);
                            }
                        }
                    }
                }

                _depthStencil.IsDirty = false;
                renderTargetsNeedUpdate = true;
            }
            else if (_couldBarriersHaveChanged)
            {
                TextureHandle? depthStencil = _depthStencil.Value;
                if (depthStencil.HasValue)
                {
                    barrierGroup.RequestBarrier(depthStencil.GetValueOrDefault(), commandRecorder.CommandOffset, ResourceBarrierSync.DepthStencil, ResourceBarrierAccess.DepthStencilWrite);
                }
            }

            if (renderTargetsNeedUpdate)
            {
                TextureHandle? depthStencil = _depthStencil.Value;

                RHIFormat format = RHIFormat.Unknown;
                if (depthStencil.HasValue)
                {
                    ref TextureHandle handle = ref depthStencil.DangerousGetValueOrDefaultReference();
                    format = handle.IsImported ?
                        _frameGraph.ResourceManager.GetRHIResource(handle).Description.Format :
                        _frameGraph.ResourceManager.GetDescription(handle).Format;
                }

                commandRecorder.RecordCommand(CmdCommandType.CommitRenderTargets, new CmdCommitRenderTargets
                {
                    DepthStencilFormat = format,
                    DepthStencil = !_depthStencil.Value.HasValue ? CPUDescriptorCmdData.Null : descriptorAllocator.GetDescriptorForDepthStencil(_depthStencil.Value.GetValueOrDefault()),
                    StartSlot = (byte)_renderTargetsRange.Start,
                    NumRenderTargets = (byte)_renderTargetsRange.Length
                });
            }

            if (_viewports.IsAnyDirty)
            {
                int count = _viewports.DirtyCount;

                int startIndex = -1;
                int endIndex = -1;

                for (int i = 0; i < count; ++i)
                {
                    FGViewport? viewport = _viewports[i];
                    if (_viewports.IsDirty(i))
                    {
                        if (!viewport.HasValue)
                        {
                            TextureHandle? rtTex = _renderTargets[i];
                            if (rtTex.HasValue)
                            {
                                ref TextureHandle handle = ref rtTex.DangerousGetValueOrDefaultReference();
                                if (handle.IsImported)
                                {
                                    ref readonly RHITextureDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(handle).Description;
                                    _viewports[0] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                                }
                                else
                                {
                                    ref readonly FGTextureDesc desc = ref _frameGraph.ResourceManager.GetDescription(handle);
                                    _viewports[0] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                                }
                            }
                            else if (i == 0 && _depthStencil.Value.HasValue)
                            {
                                TextureHandle handle = _depthStencil.Value.GetValueOrDefault();
                                if (handle.IsImported)
                                {
                                    ref readonly RHITextureDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(handle).Description;
                                    _viewports[0] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                                }
                                else
                                {
                                    ref readonly FGTextureDesc desc = ref _frameGraph.ResourceManager.GetDescription(handle);
                                    _viewports[0] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                                }
                            }
                        }

                        if (viewport.HasValue)
                        {
                            commandRecorder.RecordCommand(CmdCommandType.SetViewport, new CmdSetViewport { Viewport = viewport.GetValueOrDefault(), Slot = (byte)i });
                        }
                    }

                    if (viewport.HasValue)
                    {
                        if (startIndex == -1)
                            startIndex = i;
                        endIndex = i;
                    }
                }

                ++endIndex;
                if (startIndex != endIndex)
                {
                    if (startIndex != -1)
                    {
                        commandRecorder.RecordCommand(CmdCommandType.CommitViewports, new CmdCommitViewports { StartSlot = (byte)startIndex, NumViewportsToSet = (byte)(endIndex - startIndex) });
                    }
                    else
                    {
                        commandRecorder.RecordCommand(CmdCommandType.CommitViewports, new CmdCommitViewports { });
                    }
                }

                _viewports.ClearDirty();
            }

            if (_scissors.IsAnyDirty)
            {
                int count = _scissors.DirtyCount;

                int startIndex = -1;
                int endIndex = -1;

                for (int i = 0; i < count; ++i)
                {
                    FGRect? rect = _scissors[i];
                    if (_scissors.IsDirty(i))
                    {
                        if (!rect.HasValue)
                        {
                            TextureHandle? rtTex = _renderTargets[i];
                            if (rtTex.HasValue)
                            {
                                ref TextureHandle handle = ref rtTex.DangerousGetValueOrDefaultReference();
                                if (handle.IsImported)
                                {
                                    ref readonly RHITextureDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(handle).Description;
                                    _scissors[0] = new FGRect(0, 0, desc.Width, desc.Height);
                                }
                                else
                                {
                                    ref readonly FGTextureDesc desc = ref _frameGraph.ResourceManager.GetDescription(handle);
                                    _scissors[0] = new FGRect(0, 0, desc.Width, desc.Height);
                                }
                            }
                            else if (i == 0 && _depthStencil.Value.HasValue)
                            {
                                TextureHandle handle = _depthStencil.Value.GetValueOrDefault();
                                if (handle.IsImported)
                                {
                                    ref readonly RHITextureDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(handle).Description;
                                    _scissors[0] = new FGRect(0, 0, desc.Width, desc.Height);
                                }
                                else
                                {
                                    ref readonly FGTextureDesc desc = ref _frameGraph.ResourceManager.GetDescription(handle);
                                    _scissors[0] = new FGRect(0, 0, desc.Width, desc.Height);
                                }
                            }
                        }

                        if (rect.HasValue)
                        {
                            commandRecorder.RecordCommand(CmdCommandType.SetScissor, new CmdSetScissor { Rect = rect.GetValueOrDefault(), Slot = (byte)i });
                        }
                    }

                    if (rect.HasValue)
                    {
                        if (startIndex == -1)
                            startIndex = i;
                        endIndex = i;
                    }
                }

                ++endIndex;
                if (startIndex != endIndex)
                {
                    if (startIndex != -1)
                    {
                        commandRecorder.RecordCommand(CmdCommandType.CommitScissors, new CmdCommitViewports { StartSlot = (byte)startIndex, NumViewportsToSet = (byte)(endIndex - startIndex) });
                    }
                    else
                    {
                        commandRecorder.RecordCommand(CmdCommandType.CommitScissors, new CmdCommitViewports { });
                    }
                }

                _scissors.ClearDirty();
            }

            if (_stencilRef.IsDirty)
            {
                commandRecorder.RecordCommand(CmdCommandType.SetStencilRef, new CmdSetStencilRef { StencilRef = _stencilRef.Value });
                _stencilRef.IsDirty = false;
            }

            if (_blendFactor.IsDirty)
            {
                commandRecorder.RecordCommand(CmdCommandType.SetStencilRef, new CmdSetBlendFactor { Color = _blendFactor.Value });
                _blendFactor.IsDirty = false;
            }

            if (_vertexBuffer.IsDirty)
            {
                SetVertexBufferData? buffer = _vertexBuffer.Value;
                if (buffer.HasValue)
                {
                    SetVertexBufferData value = buffer.GetValueOrDefault();

                    ushort byteStride = (ushort)value.Stride;
                    uint byteWidth = 0;

                    if (value.Buffer.IsImported)
                    {
                        ref readonly RHIBufferDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(value.Buffer).Description;
                        byteWidth = desc.Width;

                        if (byteStride == 0)
                            byteStride = (ushort)desc.Stride;
                    }
                    else
                    {
                        ref readonly FGBufferDesc desc = ref _frameGraph.ResourceManager.GetDescription(value.Buffer);
                        byteWidth = (uint)(desc.ElementCount * desc.Stride);

                        if (byteStride == 0)
                            byteStride = (ushort)desc.Stride;
                    }

                    barrierGroup.RequestBarrier(value.Buffer, commandRecorder.CommandOffset, ResourceBarrierSync.VertexShading, ResourceBarrierAccess.VertexBuffer);
                    commandRecorder.RecordCommand(CmdCommandType.SetVertexBuffer, new CmdSetVertexBuffer
                    {
                        Buffer = value.Buffer,
                        ByteWidth = byteWidth,
                        ByteStride = byteStride
                    });
                }

                _vertexBuffer.IsDirty = false;
            }
            else if (_couldBarriersHaveChanged)
            {
                SetVertexBufferData? buffer = _vertexBuffer.Value;
                if (buffer.HasValue)
                {
                    barrierGroup.RequestBarrier(buffer.GetValueOrDefault().Buffer, commandRecorder.CommandOffset, ResourceBarrierSync.VertexShading, ResourceBarrierAccess.VertexBuffer);
                }
            }

            if (_indexBuffer.IsDirty)
            {
                SetIndexBufferData? buffer = _indexBuffer.Value;
                if (buffer.HasValue)
                {
                    SetIndexBufferData value = buffer.GetValueOrDefault();

                    ushort byteStride = (ushort)value.Stride;
                    uint byteWidth = 0;

                    if (value.Buffer.IsImported)
                    {
                        ref readonly RHIBufferDescription desc = ref _frameGraph.ResourceManager.GetRHIResource(value.Buffer).Description;
                        byteWidth = desc.Width;

                        if (byteStride == 0)
                            byteStride = (ushort)desc.Stride;
                    }
                    else
                    {
                        ref readonly FGBufferDesc desc = ref _frameGraph.ResourceManager.GetDescription(value.Buffer);
                        byteWidth = (uint)(desc.ElementCount * desc.Stride);

                        if (byteStride == 0)
                            byteStride = (ushort)desc.Stride;
                    }

                    barrierGroup.RequestBarrier(value.Buffer, commandRecorder.CommandOffset, ResourceBarrierSync.IndexInput, ResourceBarrierAccess.IndexBuffer);
                    commandRecorder.RecordCommand(CmdCommandType.SetIndexBuffer, new CmdSetIndexBuffer
                    {
                        Buffer = value.Buffer,
                        ByteWidth = byteWidth,
                        ByteStride = byteStride
                    });
                }

                _indexBuffer.IsDirty = false;
            }
            else if (_couldBarriersHaveChanged)
            {
                SetIndexBufferData? buffer = _indexBuffer.Value;
                if (buffer.HasValue)
                {
                    barrierGroup.RequestBarrier(buffer.GetValueOrDefault().Buffer, commandRecorder.CommandOffset, ResourceBarrierSync.IndexInput, ResourceBarrierAccess.IndexBuffer);
                }
            }

            if (_shader.IsDirty || renderTargetsNeedUpdate)
            {
                if (_shader.IsDirty)
                    SetPipelineLimits(shader);

                commandRecorder.RecordCommand(CmdCommandType.SetPipeline, new CmdSetPipeline { PipelineIndex = _shader.Value });
                _shader.IsDirty = false;
            }

            return base.TryCommitState(ref stats, barrierGroup, descriptorAllocator, commandRecorder, objectsCache);
        }

        internal void SetRenderTarget(int slot, TextureHandle? texture) => _renderTargets[slot] = texture;
        internal void SetDepthStencil(TextureHandle? texture) => _depthStencil.Value = texture;

        internal void SetViewport(int slot, FGViewport? viewport) => _viewports[slot] = viewport;
        internal void SetScissor(int slot, FGRect? scissor) => _scissors[slot] = scissor;

        internal void SetStencilRef(uint stencilRef) => _stencilRef.Value = stencilRef;
        internal void SetBlendFactor(Color color) => _blendFactor.Value = color;

        internal void SetVertexBuffer(BufferHandle? buffer, int stride) => _vertexBuffer.Value = buffer.HasValue ? new SetVertexBufferData(buffer.Value, stride) : null;
        internal void SetIndexBuffer(BufferHandle? buffer, int stride) => _indexBuffer.Value = buffer.HasValue ? new SetIndexBufferData(buffer.Value, stride) : null;

        internal void SetShader(int shaderIndex) => _shader.Value = shaderIndex;

        private readonly record struct NullableUnique<T>(T Value, bool HasValue) : IEquatable<NullableUnique<T>> where T : struct, IEquatable<T>
        {
            public bool Equals(NullableUnique<T> other)
            {
                if (!HasValue)
                    return !other.HasValue;
                else if (!other.HasValue)
                    return !HasValue;
                return Value.Equals(other.Value);
            }

            public override int GetHashCode() => HasValue ? Value.GetHashCode() : 0;
            public override string? ToString() => HasValue ? Value.ToString() : "null";

            public static implicit operator NullableUnique<T>(T? value) => new NullableUnique<T>(value.GetValueOrDefault(), value.HasValue);
            public static implicit operator T?(NullableUnique<T> value) => value.HasValue ? value.Value : null;
        }
    }

    internal readonly record struct SetVertexBufferData(BufferHandle Buffer, int Stride);
    internal readonly record struct SetIndexBufferData(BufferHandle Buffer, int Stride);
}
