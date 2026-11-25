using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Datatypes;
using Primary.Rendering.Memory;
using Primary.Rendering.Recording;
using Primary.Rendering.State;
using Primary.Rendering.Statistics;
using Primary.RHI;
using TerraFX.Interop.Windows;
using static Primary.Rendering.Resources.FGResourceDesc;

namespace Primary.Rendering
{
    public readonly record struct RasterCommandBuffer
    {
        private readonly FrameGraph _frameGraph;
        private readonly CompiledRenderPass _renderPass;

        private readonly CommandRecorder _commandRecorder;
        private readonly BarrierGroup _barrierGroup;
        private readonly DescriptorAllocator _descriptorAllocator;
        private readonly ObjectsCache _objectsCache;

        private readonly RasterState _rasterState;

        internal RasterCommandBuffer(FrameGraph frameGraph, CompiledRenderPass renderPass, DescriptorAllocator descriptorAllocator, RasterState rasterState)
        {
            _frameGraph = frameGraph;
            _renderPass = renderPass;

            _commandRecorder = renderPass.Recorder;
            _barrierGroup = renderPass.BarrierGroup;
            _descriptorAllocator = descriptorAllocator;
            _objectsCache = renderPass.ObjectsCache;

            _rasterState = rasterState;
        }

        internal readonly void SetSwapChain(RHISwapChain swapChain)
        {
            TextureHandle handle = _frameGraph.ImportTextureAsPure(swapChain.Backbuffers[swapChain.CurrentBackbufferIndex]);

            _rasterState.SetRenderTarget(0, handle);
            _frameGraph.UseSwapChain(swapChain);
        }

        #region Clear
        public readonly void ClearRenderTarget(TextureHandle texture)
        {
            CPUDescriptorCmdData descriptor = _descriptorAllocator.GetDescriptorForRenderTarget(texture);

            _barrierGroup.RequestBarrier(texture, _commandRecorder.CommandOffset, ResourceBarrierSync.RenderTarget, ResourceBarrierAccess.RenderTarget);
            _commandRecorder.RecordCommand(CmdCommandType.ClearRenderTarget, new CmdClearRenderTarget { RenderTarget = descriptor });

            _rasterState.NotifyOfPossibleBarrierChange();
            _renderPass.InitializeRenderTarget(texture);
        }

        public readonly void ClearRenderTarget(TextureHandle texture, Color color)
        {
            CPUDescriptorCmdData descriptor = _descriptorAllocator.GetDescriptorForRenderTarget(texture);

            _barrierGroup.RequestBarrier(texture, _commandRecorder.CommandOffset, ResourceBarrierSync.RenderTarget, ResourceBarrierAccess.RenderTarget);
            _commandRecorder.RecordCommand(CmdCommandType.ClearRenderTarget, new CmdClearRenderTarget { RenderTarget = descriptor, ClearColor = color });

            _rasterState.NotifyOfPossibleBarrierChange();
            _renderPass.InitializeRenderTarget(texture);
        }

        public readonly void ClearRenderTarget(TextureHandle texture, Color color, ReadOnlySpan<FGRect> rect)
        {
            CPUDescriptorCmdData descriptor = _descriptorAllocator.GetDescriptorForRenderTarget(texture);
            int rectCount = Math.Min(rect.Length, 8);

            _barrierGroup.RequestBarrier(texture, _commandRecorder.CommandOffset, ResourceBarrierSync.RenderTarget, ResourceBarrierAccess.RenderTarget);
            _commandRecorder.RecordCommand(CmdCommandType.ClearRenderTarget, new CmdClearRenderTarget { RenderTarget = descriptor, ClearColor = color, RectCount = (byte)rectCount });
            _commandRecorder.RecordCommandData(rect[..rectCount]);

            _rasterState.NotifyOfPossibleBarrierChange();
            _renderPass.InitializeRenderTarget(texture);
        }

        public readonly void ClearDepthStencil(TextureHandle texture)
        {
            CPUDescriptorCmdData descriptor = _descriptorAllocator.GetDescriptorForDepthStencil(texture);

            _barrierGroup.RequestBarrier(texture, _commandRecorder.CommandOffset, ResourceBarrierSync.DepthStencil, ResourceBarrierAccess.DepthStencilWrite);
            _commandRecorder.RecordCommand(CmdCommandType.ClearDepthStencil, new CmdClearDepthStencil { DepthStencil = descriptor });

            _rasterState.NotifyOfPossibleBarrierChange();
            _renderPass.InitializeDepthStencil(texture);
        }

        public readonly void ClearDepthStencil(TextureHandle texture, float depth, byte stencil)
        {
            CPUDescriptorCmdData descriptor = _descriptorAllocator.GetDescriptorForDepthStencil(texture);

            _barrierGroup.RequestBarrier(texture, _commandRecorder.CommandOffset, ResourceBarrierSync.DepthStencil, ResourceBarrierAccess.DepthStencilWrite);
            _commandRecorder.RecordCommand(CmdCommandType.ClearDepthStencil, new CmdClearDepthStencil { DepthStencil = descriptor, Depth = depth, Stencil = stencil });

            _rasterState.NotifyOfPossibleBarrierChange();
            _renderPass.InitializeDepthStencil(texture);
        }

        public readonly void ClearDepthStencil(TextureHandle texture, float depth, byte stencil, ReadOnlySpan<FGRect> rect)
        {
            CPUDescriptorCmdData descriptor = _descriptorAllocator.GetDescriptorForDepthStencil(texture);
            int rectCount = Math.Min(rect.Length, 8);

            _barrierGroup.RequestBarrier(texture, _commandRecorder.CommandOffset, ResourceBarrierSync.DepthStencil, ResourceBarrierAccess.DepthStencilWrite);
            _commandRecorder.RecordCommand(CmdCommandType.ClearDepthStencil, new CmdClearDepthStencil { DepthStencil = descriptor, Depth = depth, Stencil = stencil, RectCount = (byte)rectCount });
            _commandRecorder.RecordCommandData(rect[..rectCount]);

            _rasterState.NotifyOfPossibleBarrierChange();
            _renderPass.InitializeDepthStencil(texture);
        }

        public readonly void ClearUnorderedAccessView(ResourceHandle resource, float value)
        {
            DescriptorData descriptorData = _descriptorAllocator.GetDescriptorForResourceViewCPUGPU(DescriptorHeapType.UnorderedAccessView, resource, out bool updateDescriptorHeap);
            if (updateDescriptorHeap)
                _descriptorAllocator.RecordDescriptorSets(_commandRecorder);

            InlineArray4<float> values = new InlineArray4<float>();
            values[0] = value;
            values[1] = value;
            values[2] = value;
            values[3] = value;

            _barrierGroup.RequestBarrier(resource, _commandRecorder.CommandOffset, ResourceBarrierSync.ClearUnorderedAccess, ResourceBarrierAccess.UnorderedAccess);
            _commandRecorder.RecordCommand(CmdCommandType.ClearUAVFloat, new CmdClearUAVFloat { ResourceIndex = resource.Index, ResourceCPU = descriptorData, ResourceGPU = descriptorData, Value = values });

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void ClearUnorderedAccessView(ResourceHandle resource, float value, ReadOnlySpan<FGRect> rect)
        {
            DescriptorData descriptorData = _descriptorAllocator.GetDescriptorForResourceViewCPUGPU(DescriptorHeapType.UnorderedAccessView, resource, out bool updateDescriptorHeap);
            if (updateDescriptorHeap)
                _descriptorAllocator.RecordDescriptorSets(_commandRecorder);

            InlineArray4<float> values = new InlineArray4<float>();
            values[0] = value;
            values[1] = value;
            values[2] = value;
            values[3] = value;

            int rectCount = Math.Min(rect.Length, 8);

            _barrierGroup.RequestBarrier(resource, _commandRecorder.CommandOffset, ResourceBarrierSync.ClearUnorderedAccess, ResourceBarrierAccess.UnorderedAccess);
            _commandRecorder.RecordCommand(CmdCommandType.ClearUAVFloat, new CmdClearUAVFloat { ResourceIndex = resource.Index, ResourceCPU = descriptorData, ResourceGPU = descriptorData, Value = values, RectCount = (byte)rectCount });
            _commandRecorder.RecordCommandData(rect[..rectCount]);

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void ClearUnorderedAccessView(ResourceHandle resource, InlineArray4<float> values)
        {
            DescriptorData descriptorData = _descriptorAllocator.GetDescriptorForResourceViewCPUGPU(DescriptorHeapType.UnorderedAccessView, resource, out bool updateDescriptorHeap);
            if (updateDescriptorHeap)
                _descriptorAllocator.RecordDescriptorSets(_commandRecorder);

            _barrierGroup.RequestBarrier(resource, _commandRecorder.CommandOffset, ResourceBarrierSync.ClearUnorderedAccess, ResourceBarrierAccess.UnorderedAccess);
            _commandRecorder.RecordCommand(CmdCommandType.ClearUAVFloat, new CmdClearUAVFloat { ResourceIndex = resource.Index, ResourceCPU = descriptorData, ResourceGPU = descriptorData, Value = values });

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void ClearUnorderedAccessView(ResourceHandle resource, InlineArray4<float> values, ReadOnlySpan<FGRect> rect)
        {
            DescriptorData descriptorData = _descriptorAllocator.GetDescriptorForResourceViewCPUGPU(DescriptorHeapType.UnorderedAccessView, resource, out bool updateDescriptorHeap);
            if (updateDescriptorHeap)
                _descriptorAllocator.RecordDescriptorSets(_commandRecorder);

            int rectCount = Math.Min(rect.Length, 8);

            _barrierGroup.RequestBarrier(resource, _commandRecorder.CommandOffset, ResourceBarrierSync.ClearUnorderedAccess, ResourceBarrierAccess.UnorderedAccess);
            _commandRecorder.RecordCommand(CmdCommandType.ClearUAVFloat, new CmdClearUAVFloat { ResourceIndex = resource.Index, ResourceCPU = descriptorData, ResourceGPU = descriptorData, Value = values, RectCount = (byte)rectCount });
            _commandRecorder.RecordCommandData(rect[..rectCount]);

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void ClearUnorderedAccessView(ResourceHandle resource, uint value)
        {
            DescriptorData descriptorData = _descriptorAllocator.GetDescriptorForResourceViewCPUGPU(DescriptorHeapType.UnorderedAccessView, resource, out bool updateDescriptorHeap);
            if (updateDescriptorHeap)
                _descriptorAllocator.RecordDescriptorSets(_commandRecorder);

            InlineArray4<uint> values = new InlineArray4<uint>();
            values[0] = value;
            values[1] = value;
            values[2] = value;
            values[3] = value;

            _barrierGroup.RequestBarrier(resource, _commandRecorder.CommandOffset, ResourceBarrierSync.ClearUnorderedAccess, ResourceBarrierAccess.UnorderedAccess);
            _commandRecorder.RecordCommand(CmdCommandType.ClearUAVUInt, new CmdClearUAVUInt { ResourceIndex = resource.Index, ResourceCPU = descriptorData, ResourceGPU = descriptorData, Value = values });

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void ClearUnorderedAccessView(ResourceHandle resource, uint value, ReadOnlySpan<FGRect> rect)
        {
            DescriptorData descriptorData = _descriptorAllocator.GetDescriptorForResourceViewCPUGPU(DescriptorHeapType.UnorderedAccessView, resource, out bool updateDescriptorHeap);
            if (updateDescriptorHeap)
                _descriptorAllocator.RecordDescriptorSets(_commandRecorder);

            InlineArray4<uint> values = new InlineArray4<uint>();
            values[0] = value;
            values[1] = value;
            values[2] = value;
            values[3] = value;

            int rectCount = Math.Min(rect.Length, 8);

            _barrierGroup.RequestBarrier(resource, _commandRecorder.CommandOffset, ResourceBarrierSync.ClearUnorderedAccess, ResourceBarrierAccess.UnorderedAccess);
            _commandRecorder.RecordCommand(CmdCommandType.ClearUAVUInt, new CmdClearUAVUInt { ResourceIndex = resource.Index, ResourceCPU = descriptorData, ResourceGPU = descriptorData, Value = values, RectCount = (byte)rectCount });
            _commandRecorder.RecordCommandData(rect[..rectCount]);

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void ClearUnorderedAccessView(ResourceHandle resource, InlineArray4<uint> values)
        {
            DescriptorData descriptorData = _descriptorAllocator.GetDescriptorForResourceViewCPUGPU(DescriptorHeapType.UnorderedAccessView, resource, out bool updateDescriptorHeap);
            if (updateDescriptorHeap)
                _descriptorAllocator.RecordDescriptorSets(_commandRecorder);

            _barrierGroup.RequestBarrier(resource, _commandRecorder.CommandOffset, ResourceBarrierSync.ClearUnorderedAccess, ResourceBarrierAccess.UnorderedAccess);
            _commandRecorder.RecordCommand(CmdCommandType.ClearUAVUInt, new CmdClearUAVUInt { ResourceIndex = resource.Index, ResourceCPU = descriptorData, ResourceGPU = descriptorData, Value = values });

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void ClearUnorderedAccessView(ResourceHandle resource, InlineArray4<uint> values, ReadOnlySpan<FGRect> rect)
        {
            DescriptorData descriptorData = _descriptorAllocator.GetDescriptorForResourceViewCPUGPU(DescriptorHeapType.UnorderedAccessView, resource, out bool updateDescriptorHeap);
            if (updateDescriptorHeap)
                _descriptorAllocator.RecordDescriptorSets(_commandRecorder);

            int rectCount = Math.Min(rect.Length, 8);

            _barrierGroup.RequestBarrier(resource, _commandRecorder.CommandOffset, ResourceBarrierSync.ClearUnorderedAccess, ResourceBarrierAccess.UnorderedAccess);
            _commandRecorder.RecordCommand(CmdCommandType.ClearUAVUInt, new CmdClearUAVUInt { ResourceIndex = resource.Index, ResourceCPU = descriptorData, ResourceGPU = descriptorData, Value = values, RectCount = (byte)rectCount });
            _commandRecorder.RecordCommandData(rect[..rectCount]);

            _rasterState.NotifyOfPossibleBarrierChange();
        }
        #endregion

        #region Setters
        public readonly void SetRenderTarget(TextureHandle? texture)
            => _rasterState.SetRenderTarget(0, texture);

        public readonly void SetRenderTarget(int slot, TextureHandle? texture)
            => _rasterState.SetRenderTarget(slot, texture);

        public readonly void SetDepthStencil(TextureHandle? texture)
            => _rasterState.SetDepthStencil(texture);

        public readonly void SetViewport(FGViewport? viewport) => _rasterState.SetViewport(0, viewport);
        public readonly void SetViewport(int slot, FGViewport? viewport) => _rasterState.SetViewport(slot, viewport);

        public readonly void SetScissor(FGRect? scissor) => _rasterState.SetScissor(0, scissor);
        public readonly void SetScissor(int slot, FGRect? scissor) => _rasterState.SetScissor(slot, scissor);

        public readonly void SetStencilRef(uint stencilRef) => _rasterState.SetStencilRef(stencilRef);

        public readonly void SetBlendFactor(Color color) => _rasterState.SetBlendFactor(color);

        public readonly void SetShader(ShaderAsset shader)
        {
            int shaderIndex = shader.IsReady ? _objectsCache.GetIndexForObject(shader.GraphicsPipeline!) : -1;
            _rasterState.SetShader(shaderIndex);
        }

        public readonly void SetProperties(PropertyBlock propertyBlock) => _rasterState.SetPropertyBlock(propertyBlock);
        public readonly void SetProperties(ROPropertyBlock propertyBlock)
        {
            if (propertyBlock.InternalBlock != null)
                _rasterState.SetPropertyBlock(propertyBlock.InternalBlock);
        }

        public readonly void SetConstants<T>(T value) where T : unmanaged
        {
            if (Unsafe.SizeOf<T>() < 4)
            {
                uint u32 = 0;
                Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref u32), value);
                _rasterState.SetConstants(new ReadOnlySpan<uint>(ref u32));
            }
            else
            {
                int count = Unsafe.SizeOf<T>() / 4;
                _rasterState.SetConstants(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, uint>(ref value), Math.Min(count, 32)));
            }
        }

        public readonly void SetConstants<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            if (Unsafe.SizeOf<T>() * values.Length < 4)
            {
                uint u32 = 0;
                MemoryMarshal.Cast<T, byte>(values).CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<uint, byte>(ref u32), 4));
                _rasterState.SetConstants(new ReadOnlySpan<uint>(ref u32));
            }
            else
            {
                int count = (Unsafe.SizeOf<T>() * values.Length) / 4;
                _rasterState.SetConstants(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, uint>(ref values.DangerousGetReference()), Math.Min(count, 32)));
            }
        }

        public readonly void SetVertexBuffer(BufferHandle? buffer, int stride = 0) => _rasterState.SetVertexBuffer(buffer, stride);

        public readonly void SetVertexBuffer(RHIBuffer? buffer, int stride = 0)
        {
            if (buffer == null)
            {
                _rasterState.SetVertexBuffer(null, 0);
            }
            else
            {
                BufferHandle handle = _frameGraph.ImportBufferAsPure(buffer);
                _rasterState.SetVertexBuffer(handle, stride);
            }
        }

        public readonly void SetIndexBuffer(BufferHandle? buffer, int stride = 0) => _rasterState.SetIndexBuffer(buffer, stride);

        public readonly void SetIndexBuffer(RHIBuffer? buffer, int stride = 0)
        {
            if (buffer == null)
            {
                _rasterState.SetIndexBuffer(null, 0);
            }
            else
            {
                BufferHandle handle = _frameGraph.ImportBufferAsPure(buffer);
                _rasterState.SetIndexBuffer(handle, stride);
            }
        }
        #endregion

        #region Draw
        public readonly void DrawInstanced(uint vertexCountPerInstance, uint instanceCount, uint startVertexLocation, uint startInstanceLocation)
        {
            if (vertexCountPerInstance == 0 || instanceCount == 0)
                return;

            if (_rasterState.TryCommitState(ref _renderPass.Stats, _barrierGroup, _descriptorAllocator, _commandRecorder, _objectsCache))
            {
                _commandRecorder.RecordCommand(CmdCommandType.DrawInstanced, new CmdDrawInstanced
                {
                    VertexCountPerInstance = vertexCountPerInstance,
                    InstanceCount = instanceCount,
                    StartVertexLocation = startVertexLocation,
                    StartInstanceLocation = startInstanceLocation
                });

                ref RenderPassStats stats = ref _renderPass.Stats;
                ++stats.DrawCallsIssued;
                stats.TotalVertexCount += (int)(vertexCountPerInstance * instanceCount);
                stats.TotalInstanceCount += (int)instanceCount;
            }
        }

        public readonly void DrawIndexedInstanced(uint indexCountPerInstance, uint instanceCount, uint startIndexLocation, int baseVertexLocation, uint startInstanceLocation)
        {
            if (indexCountPerInstance == 0 || instanceCount == 0)
                return;

            if (_rasterState.TryCommitState(ref _renderPass.Stats, _barrierGroup, _descriptorAllocator, _commandRecorder, _objectsCache))
            {
                _commandRecorder.RecordCommand(CmdCommandType.DrawIndexedInstanced, new CmdDrawIndexedInstanced
                {
                    IndexCountPerInstance = indexCountPerInstance,
                    InstanceCount = instanceCount,
                    StartIndexLocation = startIndexLocation,
                    BaseVertexLocation = baseVertexLocation,
                    StartInstanceLocation = startInstanceLocation
                });

                ref RenderPassStats stats = ref _renderPass.Stats;
                ++stats.DrawCallsIssued;
                stats.TotalIndexCount += (int)(indexCountPerInstance * instanceCount);
                stats.TotalInstanceCount += (int)instanceCount;
            }
        }
        #endregion

        #region Operation
        public readonly void CopyBuffer(BufferHandle dst, ulong dstOffset, BufferHandle src, ulong srcOffset, ulong numBytes)
        {
            if (numBytes > 0)
            {
                _barrierGroup.RequestBarrier(src, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopySource);
                _barrierGroup.RequestBarrier(dst, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);

                _commandRecorder.RecordCommand(CmdCommandType.CopyBuffer, new CmdCopyBuffer
                {
                    Source = src,
                    Destination = dst,
                    SourceOffset = srcOffset,
                    DestinationOffset = dstOffset,
                    NumBytes = numBytes
                });

                _rasterState.NotifyOfPossibleBarrierChange();
            }
        }

        public readonly void CopyTexture(ResourceHandle dst, TextureCopyFootprint dstFootprint, Int3 dstOffset, TextureHandle src, uint subresourceIndex, FGBox? box)
        {
            _barrierGroup.RequestBarrier(src, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopySource);
            _barrierGroup.RequestBarrier(dst, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);

            CmdCopyTexture.Footprint footprintForDst;
            if (dstFootprint.FootprintType == TextureCopyFootprint.Type.Texture)
            {
                footprintForDst = new CmdCopyTexture.Footprint
                {
                    Type = TextureCopyFootprint.Type.Texture,
                    SubresourceIndex = dstFootprint.SubresourceIndex
                };
            }
            else
            {
                footprintForDst = new CmdCopyTexture.Footprint
                {
                    Type = TextureCopyFootprint.Type.Buffer,
                    PlacedFootprint = new CmdCopyTexture.PlacedFootprint
                    {
                        DataOffset = dstFootprint.Footprint.BufferOffset,
                        Format = dstFootprint.Footprint.Format,
                        Width = (ushort)dstFootprint.Footprint.Width,
                        Height = (ushort)dstFootprint.Footprint.Height,
                        Depth = (ushort)dstFootprint.Footprint.Depth,
                        RowPitch = dstFootprint.Footprint.RowPitch,
                    }
                };
            }

            _commandRecorder.RecordCommand(CmdCommandType.CopyTexture, new CmdCopyTexture
            {
                Source = src,
                Destination = dst,
                DestinationOffset = dstOffset,
                SourceBox = box,
                SourceFootprint = new CmdCopyTexture.Footprint { Type = TextureCopyFootprint.Type.Texture, SubresourceIndex = subresourceIndex },
                DestinationFootprint = footprintForDst
            });

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void CopyTexture(TextureHandle dst, uint subresourceIndex, Int3 dstOffset, ResourceHandle src, TextureCopyFootprint srcFootprint, FGBox? box)
        {
            _barrierGroup.RequestBarrier(src, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopySource);
            _barrierGroup.RequestBarrier(dst, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);

            CmdCopyTexture.Footprint footprintForSrc;
            if (srcFootprint.FootprintType == TextureCopyFootprint.Type.Texture)
            {
                footprintForSrc = new CmdCopyTexture.Footprint
                {
                    Type = TextureCopyFootprint.Type.Texture,
                    SubresourceIndex = srcFootprint.SubresourceIndex
                };
            }
            else
            {
                footprintForSrc = new CmdCopyTexture.Footprint
                {
                    Type = TextureCopyFootprint.Type.Buffer,
                    PlacedFootprint = new CmdCopyTexture.PlacedFootprint
                    {
                        DataOffset = srcFootprint.Footprint.BufferOffset,
                        Format = srcFootprint.Footprint.Format,
                        Width = (ushort)srcFootprint.Footprint.Width,
                        Height = (ushort)srcFootprint.Footprint.Height,
                        Depth = (ushort)srcFootprint.Footprint.Depth,
                        RowPitch = srcFootprint.Footprint.RowPitch,
                    }
                };
            }

            _commandRecorder.RecordCommand(CmdCommandType.CopyTexture, new CmdCopyTexture
            {
                Source = src,
                Destination = dst,
                DestinationOffset = dstOffset,
                SourceBox = box,
                SourceFootprint = footprintForSrc,
                DestinationFootprint = new CmdCopyTexture.Footprint { Type = TextureCopyFootprint.Type.Texture, SubresourceIndex = subresourceIndex }
            });

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void CopyTexture(ResourceHandle dst, TextureCopyFootprint dstFootprint, Int3 dstOffset, ResourceHandle src, TextureCopyFootprint srcFootprint, FGBox? box)
        {
            _barrierGroup.RequestBarrier(src, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopySource);
            _barrierGroup.RequestBarrier(dst, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);

            CmdCopyTexture.Footprint footprintForDst;
            if (dstFootprint.FootprintType == TextureCopyFootprint.Type.Texture)
            {
                footprintForDst = new CmdCopyTexture.Footprint
                {
                    Type = TextureCopyFootprint.Type.Texture,
                    SubresourceIndex = dstFootprint.SubresourceIndex
                };
            }
            else
            {
                footprintForDst = new CmdCopyTexture.Footprint
                {
                    Type = TextureCopyFootprint.Type.Buffer,
                    PlacedFootprint = new CmdCopyTexture.PlacedFootprint
                    {
                        DataOffset = dstFootprint.Footprint.BufferOffset,
                        Format = dstFootprint.Footprint.Format,
                        Width = (ushort)dstFootprint.Footprint.Width,
                        Height = (ushort)dstFootprint.Footprint.Height,
                        Depth = (ushort)dstFootprint.Footprint.Depth,
                        RowPitch = dstFootprint.Footprint.RowPitch,
                    }
                };
            }

            CmdCopyTexture.Footprint footprintForSrc;
            if (srcFootprint.FootprintType == TextureCopyFootprint.Type.Texture)
            {
                footprintForSrc = new CmdCopyTexture.Footprint
                {
                    Type = TextureCopyFootprint.Type.Texture,
                    SubresourceIndex = srcFootprint.SubresourceIndex
                };
            }
            else
            {
                footprintForSrc = new CmdCopyTexture.Footprint
                {
                    Type = TextureCopyFootprint.Type.Buffer,
                    PlacedFootprint = new CmdCopyTexture.PlacedFootprint
                    {
                        DataOffset = srcFootprint.Footprint.BufferOffset,
                        Format = srcFootprint.Footprint.Format,
                        Width = (ushort)srcFootprint.Footprint.Width,
                        Height = (ushort)srcFootprint.Footprint.Height,
                        Depth = (ushort)srcFootprint.Footprint.Depth,
                        RowPitch = srcFootprint.Footprint.RowPitch,
                    }
                };
            }

            _commandRecorder.RecordCommand(CmdCommandType.CopyTexture, new CmdCopyTexture
            {
                Source = src,
                Destination = dst,
                DestinationOffset = dstOffset,
                SourceBox = box,
                SourceFootprint = footprintForSrc,
                DestinationFootprint = footprintForDst
            });

            _rasterState.NotifyOfPossibleBarrierChange();
        }

        public readonly void UploadBuffer<T>(BufferHandle buffer, uint byteOffset, in T rawData) where T : unmanaged
        {
            _barrierGroup.RequestBarrier(buffer, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);
            _rasterState.NotifyOfPossibleBarrierChange();

            ReadOnlySpan<T> span = new ReadOnlySpan<T>(in rawData);
            int uploadIndex = _frameGraph.UploadManager.RequestDataUpload(buffer, byteOffset, MemoryMarshal.Cast<T, byte>(span));

            _commandRecorder.RecordCommand(CmdCommandType.UploadResource, new CmdUploadResource
            {
                Resource = buffer,
                Slice = byteOffset,
                Box = null,
                UploadIndex = uploadIndex
            });

            ref RenderPassStats stats = ref _renderPass.Stats;
            stats.UploadMemoryUsed += Unsafe.SizeOf<T>();
        }

        public readonly void UploadBuffer<T>(BufferHandle buffer, uint byteOffset, ReadOnlySpan<T> rawData) where T : unmanaged
        {
            _barrierGroup.RequestBarrier(buffer, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);
            _rasterState.NotifyOfPossibleBarrierChange();

            int uploadIndex = _frameGraph.UploadManager.RequestDataUpload(buffer, byteOffset, MemoryMarshal.Cast<T, byte>(rawData));

            _commandRecorder.RecordCommand(CmdCommandType.UploadResource, new CmdUploadResource
            {
                Resource = buffer,
                Slice = byteOffset,
                Box = null,
                UploadIndex = uploadIndex
            });

            ref RenderPassStats stats = ref _renderPass.Stats;
            stats.UploadMemoryUsed += rawData.Length * Unsafe.SizeOf<T>();
        }

        public readonly void UploadTexture<T>(TextureHandle texture, int slice, ReadOnlySpan<T> rawData) where T : unmanaged
        {
            _barrierGroup.RequestBarrier(texture, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);
            _rasterState.NotifyOfPossibleBarrierChange();

            int uploadIndex = _frameGraph.UploadManager.RequestDataUpload(texture, (uint)slice, null, MemoryMarshal.Cast<T, byte>(rawData));

            _commandRecorder.RecordCommand(CmdCommandType.UploadResource, new CmdUploadResource
            {
                Resource = texture,
                Slice = (uint)slice,
                Box = null,
                UploadIndex = uploadIndex
            });


            ref RenderPassStats stats = ref _renderPass.Stats;
            stats.UploadMemoryUsed += rawData.Length * Unsafe.SizeOf<T>();
        }

        public readonly void UploadTexture<T>(TextureHandle texture, int slice, FGBox uploadBox, ReadOnlySpan<T> rawData) where T : unmanaged
        {
            _barrierGroup.RequestBarrier(texture, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);
            _rasterState.NotifyOfPossibleBarrierChange();

            int uploadIndex = _frameGraph.UploadManager.RequestDataUpload(texture, (uint)slice, uploadBox, MemoryMarshal.Cast<T, byte>(rawData));

            _commandRecorder.RecordCommand(CmdCommandType.UploadResource, new CmdUploadResource
            {
                Resource = texture,
                Slice = (uint)slice,
                Box = uploadBox,
                UploadIndex = uploadIndex
            });


            ref RenderPassStats stats = ref _renderPass.Stats;
            stats.UploadMemoryUsed += rawData.Length * Unsafe.SizeOf<T>();
        }

        public readonly FGMappedSubresource<T> Map<T>(BufferHandle buffer) where T : unmanaged
        {
            (ArrayPtr<T> mapData, DataMapInfo mapInfo) = _frameGraph.UploadManager.MapForWriting<T>(buffer, null);
            return new FGMappedSubresource<T>(this, mapData, mapInfo);
        }

        public readonly FGMappedSubresource<T> Map<T>(BufferHandle buffer, FGRange writeRange) where T : unmanaged
        {
            (ArrayPtr<T> mapData, DataMapInfo mapInfo) = _frameGraph.UploadManager.MapForWriting<T>(buffer, writeRange);
            return new FGMappedSubresource<T>(this, mapData, mapInfo);
        }

        public readonly FGMappedSubresource<T> Map<T>(TextureHandle texture, uint subresource) where T : unmanaged
        {
            (ArrayPtr<T> mapData, DataMapInfo mapInfo) = _frameGraph.UploadManager.MapForWriting<T>(texture, subresource, null);
            return new FGMappedSubresource<T>(this, mapData, mapInfo);
        }

        public readonly FGMappedSubresource<T> Map<T>(TextureHandle texture, uint subresource, FGBox writeBox) where T : unmanaged
        {
            (ArrayPtr<T> mapData, DataMapInfo mapInfo) = _frameGraph.UploadManager.MapForWriting<T>(texture, subresource, writeBox);
            return new FGMappedSubresource<T>(this, mapData, mapInfo);
        }

        internal readonly void Unmap(DataMapInfo mapInfo)
        {
            _barrierGroup.RequestBarrier(mapInfo.Resource, _commandRecorder.CommandOffset, ResourceBarrierSync.Copy, ResourceBarrierAccess.CopyDestination);
            _rasterState.NotifyOfPossibleBarrierChange();

            int uploadIndex = _frameGraph.UploadManager.UnmapForWriting(mapInfo);

            _commandRecorder.RecordCommand(CmdCommandType.UploadResource, new CmdUploadResource
            {
                Resource = mapInfo.Resource,
                Slice = (uint)mapInfo.DestSlice,
                Box = mapInfo.DestBox,
                UploadIndex = uploadIndex
            });

            ref RenderPassStats stats = ref _renderPass.Stats;
            stats.UploadMemoryUsed += mapInfo.UploadSize;
        }
        #endregion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TextureCopyFootprint
    {
        [FieldOffset(0)]
        public Type FootprintType;
        [FieldOffset(1)]
        public uint SubresourceIndex;
        [FieldOffset(1)]
        public TextureCopyBuffer Footprint;

        public TextureCopyFootprint(uint subresourceIndex)
        {
            FootprintType = Type.Texture;
            SubresourceIndex = subresourceIndex;
        }

        public TextureCopyFootprint(TextureCopyBuffer footprint)
        {
            FootprintType = Type.Buffer;
            Footprint = footprint;
        }

        public static implicit operator TextureCopyFootprint(int subresourceIndex) => new TextureCopyFootprint((uint)subresourceIndex);
        public static implicit operator TextureCopyFootprint(uint subresourceIndex) => new TextureCopyFootprint(subresourceIndex);
        public static implicit operator TextureCopyFootprint(TextureCopyBuffer footprint) => new TextureCopyFootprint(footprint);

        public enum Type : byte
        {
            Texture = 0,
            Buffer
        }
    }

    public struct TextureCopyBuffer
    {
        public ulong BufferOffset;
        public RHIFormat Format;
        public uint Width;
        public uint Height;
        public uint Depth;
        public uint RowPitch;

        public TextureCopyBuffer(ulong bufferOffset, RHIFormat format, uint width, uint height, uint depth = 1, uint rowPitch = uint.MaxValue)
        {
            BufferOffset = bufferOffset;
            Format = format;
            Width = width;
            Height = height;
            Depth = depth;
            RowPitch = rowPitch;
        }
    }

    public ref struct FGMappedSubresource<T> : IDisposable where T : unmanaged
    {
        // This sucks ass
        private readonly RasterCommandBuffer _raster;
        private readonly ComputeCommandBuffer _compute;
        private readonly bool _isCompute;

        private readonly ArrayPtr<T> _data;
        private readonly DataMapInfo _mapInfo;

        private bool _isDisposed;

        internal FGMappedSubresource(RasterCommandBuffer commandBuffer, ArrayPtr<T> data, DataMapInfo mapInfo)
        {
            _raster = commandBuffer;

            _data = data;
            _mapInfo = mapInfo;

            _isDisposed = false;
        }

        internal FGMappedSubresource(ComputeCommandBuffer commandBuffer, ArrayPtr<T> data, DataMapInfo mapInfo)
        {
            _compute = commandBuffer;
            _isCompute = true;

            _data = data;
            _mapInfo = mapInfo;

            _isDisposed = false;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                if (_isCompute)
                    _compute.Unmap(_mapInfo);
                else
                    _raster.Unmap(_mapInfo);
                _isDisposed = true;
            }
        }

        public readonly Span<T> Span
        {
            get
            {
                ObjectDisposedException.ThrowIf(_isDisposed, typeof(FGMappedSubresource<T>));
                return _data.AsSpan();
            }
        }
    }
}
