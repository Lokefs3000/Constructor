using Primary.Rendering.D3D12;
using Primary.Rendering.Memory;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.State
{
    internal sealed class RasterState : RenderState
    {
        private DirtyArray<FrameGraphTexture> _renderTargets;
        private DirtyValue<FrameGraphTexture> _depthStencil;

        private DirtyArray<NullableUnique<FGViewport>> _viewports;
        private DirtyArray<NullableUnique<FGRect>> _scissors;

        private DirtyValue<uint> _stencilRef;

        private DirtyValue<SetVertexBufferData> _vertexBuffer;
        private DirtyValue<SetIndexBufferData> _indexBuffer;

        private DirtyValue<int> _pipeline;

        internal RasterState() : base()
        {
            _renderTargets = new DirtyArray<FrameGraphTexture>(8, FrameGraphTexture.Invalid);
            _depthStencil = new DirtyValue<FrameGraphTexture>(FrameGraphTexture.Invalid);

            _viewports = new DirtyArray<NullableUnique<FGViewport>>(8, null);
            _scissors = new DirtyArray<NullableUnique<FGRect>>(8, null);

            _stencilRef = new DirtyValue<uint>(0);

            _vertexBuffer = new DirtyValue<SetVertexBufferData>(SetVertexBufferData.Invalid);
            _indexBuffer = new DirtyValue<SetIndexBufferData>(SetIndexBufferData.Invalid);

            _pipeline = new DirtyValue<int>();
        }

        protected override void DisposeInternal(bool disposing) { }

        internal override void ClearState()
        {
            base.ClearState();

            _renderTargets.Fill(FrameGraphTexture.Invalid);
            _depthStencil.Value = FrameGraphTexture.Invalid;

            _viewports.Fill(null);
            _scissors.Fill(null);

            _stencilRef.Value = 0;

            _vertexBuffer.Value = SetVertexBufferData.Invalid;
            _indexBuffer.Value = SetIndexBufferData.Invalid;

            _pipeline.Value = -1;
        }

        internal override bool CommitState(SequentialLinearAllocator allocator, CommandRecorder recorder)
        {
            if (_pipeline.Value == -1)
                return false;

            if (_renderTargets.IsAnyDirty)
            {
                int count = _renderTargets.DirtyCount;
                for (int i = 0; i < count; ++i)
                {
                    if (_renderTargets.IsDirty(i))
                    {
                        ref FrameGraphTexture texture = ref _renderTargets.GetWithoutDirty(i);

                        recorder.AddCommand(RecCommandType.SetRenderTarget, new CmdSetRenderTarget
                        {
                            Slot = (byte)i,
                            Texture = texture
                        });

                        if (!texture.IsNull)
                        {
                            if (!_viewports.GetWithoutDirty(i).HasValue)
                            {
                                if (texture.IsExternal)
                                {
                                    ref readonly RHITextureDescription desc = ref texture.Resource!.Description;
                                    _viewports[i] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                                }
                                else
                                {
                                    ref readonly FrameGraphTextureDesc desc = ref texture.Description;
                                    _viewports[i] = new FGViewport(0.0f, 0.0f, desc.Width, desc.Height);
                                }
                            }

                            if (!_scissors.GetWithoutDirty(i).HasValue)
                            {
                                if (texture.IsExternal)
                                {
                                    ref readonly RHITextureDescription desc = ref texture.Resource!.Description;
                                    _scissors[i] = new FGRect(0, 0, desc.Width, desc.Height);
                                }
                                else
                                {
                                    ref readonly FrameGraphTextureDesc desc = ref texture.Description;
                                    _scissors[i] = new FGRect(0, 0, desc.Width, desc.Height);
                                }
                            }
                        }
                    }
                }

                _renderTargets.ClearDirty();
            }

            if (_depthStencil.IsDirty)
            {
                recorder.AddCommand(RecCommandType.SetDepthStencil, new CmdSetDepthStencil
                {
                    Texture = _depthStencil.Value
                });

                _depthStencil.IsDirty = false;
            }

            if (_viewports.IsAnyDirty)
            {
                int count = _viewports.DirtyCount;
                for (int i = 0; i < count; ++i)
                {
                    if (_viewports.IsDirty(i))
                    {
                        ref NullableUnique<FGViewport> viewport = ref _viewports.GetWithoutDirty(i);
                        if (viewport.HasValue)
                        {
                            recorder.AddCommand(RecCommandType.SetViewport, new CmdSetViewport
                            {
                                Slot = (byte)i,
                                Viewport = viewport.Value
                            });
                        }
                    }
                }

                _viewports.ClearDirty();
            }

            if (_scissors.IsAnyDirty)
            {
                int count = _scissors.DirtyCount;
                for (int i = 0; i < count; ++i)
                {
                    if (_scissors.IsDirty(i))
                    {
                        ref NullableUnique<FGRect> scissor = ref _scissors.GetWithoutDirty(i);
                        if (scissor.HasValue)
                        {
                            recorder.AddCommand(RecCommandType.SetScissor, new CmdSetScissor
                            {
                                Slot = (byte)i,
                                Scissor = scissor.Value
                            });
                        }
                    }
                }

                _scissors.ClearDirty();
            }

            if (_stencilRef.IsDirty)
            {
                recorder.AddCommand(RecCommandType.SetStencilReference, new CmdSetStencilRef
                {
                    StencilRef = _stencilRef.Value
                });

                _stencilRef.IsDirty = false;
            }

            if (_vertexBuffer.IsDirty)
            {
                SetVertexBufferData bufferData = _vertexBuffer.Value;
                recorder.AddCommand(RecCommandType.SetVertexBuffer, new CmdSetVertexBuffer
                {
                    Resource = bufferData.Buffer,
                    Stride = (ushort)bufferData.Stride
                });

                _vertexBuffer.IsDirty = false;
            }

            if (_indexBuffer.IsDirty)
            {
                SetIndexBufferData bufferData = _indexBuffer.Value;
                recorder.AddCommand(RecCommandType.SetIndexBuffer, new CmdSetIndexBuffer
                {
                    Resource = bufferData.Buffer,
                    Stride = (ushort)bufferData.Stride
                });

                _indexBuffer.IsDirty = false;
            }

            if (_pipeline.IsDirty)
            {
                recorder.AddCommand(RecCommandType.SetPipeline, new CmdSetPipeline
                {
                    Index = _pipeline.Value
                });

                _pipeline.IsDirty = false;
            }

            return base.CommitState(allocator, recorder);
        }

        internal void SetRenderTarget(int slot, FrameGraphTexture texture) => _renderTargets[slot] = texture;
        internal void SetDepthStencil(FrameGraphTexture texture) => _depthStencil.Value = texture;

        internal void SetViewport(int slot, FGViewport? viewport) => _viewports[slot] = viewport;
        internal void SetScissor(int slot, FGRect? scissor) => _scissors[slot] = scissor;

        internal void SetStencilRef(uint stencilRef) => _stencilRef.Value = stencilRef;

        internal void SetVertexBuffer(SetVertexBufferData bufferData) => _vertexBuffer.Value = bufferData;
        internal void SetIndexBuffer(SetIndexBufferData bufferData) => _indexBuffer.Value = bufferData;

        internal void SetPipeline(int pipelineIndex) => _pipeline.Value = pipelineIndex;

        private readonly record struct NullableUnique<T>(T Value, bool HasValue) : IEquatable<NullableUnique<T>> where T : struct
        {
            public bool Equals(NullableUnique<T> other)
            {
                if (!HasValue)
                    return !other.HasValue;
                else if (other.HasValue)
                    return !HasValue;
                return Value.Equals(other.Value);
            }

            public override int GetHashCode() => HasValue ? Value.GetHashCode() : 0;
            public override string? ToString() => HasValue ? Value.ToString() : "null";

            public static implicit operator NullableUnique<T>(T? value) => new NullableUnique<T>(value.GetValueOrDefault(), value.HasValue);
            public static implicit operator T?(NullableUnique<T> value) => value.HasValue ? value.Value : null;
        }
    }

    internal readonly record struct SetVertexBufferData(FrameGraphBuffer Buffer, uint Stride)
    {
        public static readonly SetVertexBufferData Invalid = new SetVertexBufferData(FrameGraphBuffer.Invalid, 0xffffffff);
    }
    internal readonly record struct SetIndexBufferData(FrameGraphBuffer Buffer, uint Stride)
    {
        public static readonly SetIndexBufferData Invalid = new SetIndexBufferData(FrameGraphBuffer.Invalid, 0xffffffff);
    }
}
