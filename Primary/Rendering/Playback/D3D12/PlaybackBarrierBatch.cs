using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Rendering.Recording;
using Primary.RHI;
using Primary.RHI.Direct3D12;
using SharpGen.Runtime;
using Silk.NET.Direct3D12;
using BarrierGroup = Silk.NET.Direct3D12.BarrierGroup;

namespace Primary.Rendering.Playback.D3D12
{
    internal unsafe sealed class PlaybackBarrierBatch
    {
        private readonly PlaybackDevice _device;
        private readonly PlaybackExecutor _executor;

        private readonly List<BufferBarrier> _bufferBarriers;
        private readonly List<TextureBarrier> _textureBarriers;

        internal PlaybackBarrierBatch(PlaybackDevice device, PlaybackExecutor executor)
        {
            _device = device;
            _executor = executor;

            _bufferBarriers = new List<BufferBarrier>();
            _textureBarriers = new List<TextureBarrier>();
        }

        internal void AssertIsFlushed()
        {
            Debug.Assert(_bufferBarriers.Count == 0);
            Debug.Assert(_textureBarriers.Count == 0);
        }

        internal void FlushBarriers(ID3D12GraphicsCommandList10* commandList)
        {
            Span<BufferBarrier> bufferBarriers = _bufferBarriers.AsSpan();
            Span<TextureBarrier> textureBarriers = _textureBarriers.AsSpan();

            if (bufferBarriers.IsEmpty && textureBarriers.IsEmpty)
                return;

            fixed (BufferBarrier* ptr1 = bufferBarriers)
            {
                fixed (TextureBarrier* ptr2 = textureBarriers)
                {
                    BarrierGroup* groups = stackalloc BarrierGroup[2];
                    int groupCount = 0;

                    if (!bufferBarriers.IsEmpty)
                    {
                        groups[groupCount++] = new BarrierGroup
                        {
                            Type = BarrierType.Buffer,
                            NumBarriers = (uint)bufferBarriers.Length,
                            Anonymous = new BarrierGroupUnion
                            {
                                PBufferBarriers = ptr1
                            }
                        };
                    }

                    if (!textureBarriers.IsEmpty)
                    {
                        groups[groupCount++] = new BarrierGroup
                        {
                            Type = BarrierType.Texture,
                            NumBarriers = (uint)textureBarriers.Length,
                            Anonymous = new BarrierGroupUnion
                            {
                                PTextureBarriers = ptr2
                            }
                        };
                    }

                    commandList->Barrier((uint)groupCount, groups);
                }
            }

            _bufferBarriers.Clear();
            _textureBarriers.Clear();
        }

        internal void AddBarrierToBatch(in RenderBarrier barrier, CompiledRenderPlan renderPlan, CompiledRenderPass renderPass)
        {
            RenderResourceInfo resourceInfo = renderPlan.Resources[renderPlan.ResourceIndices[barrier.ResourceIndex]];
            FGResourceType resourceType = resourceInfo.ImportedResource != null ?
                (resourceInfo.ImportedResource is RHIBuffer ? FGResourceType.Buffer : FGResourceType.Texture) :
                renderPlan.ResourceDescs[resourceInfo.DescriptionIndex].Type;

            ID3D12Resource2* resource = _executor.ResourceManager.GetResourcePointer(barrier.ResourceIndex);
            if (resourceType == FGResourceType.Buffer)
            {
                _bufferBarriers.Add(new BufferBarrier(
                    ConvertEnumToSync(barrier.OldSync, barrier.OldQueueSpec), ConvertEnumToSync(barrier.NewSync, barrier.NewQueueSpec),
                    ConvertEnumToAccess(barrier.OldAccess, barrier.OldQueueSpec), ConvertEnumToAccess(barrier.NewAccess, barrier.NewQueueSpec),
                    (ID3D12Resource*)resource, null, ulong.MaxValue));
            }
            else
            {
                TextureBarrierFlags flags = TextureBarrierFlags.None;
                if (barrier.OldAccess == ResourceBarrierAccess.None &&
                    (barrier.NewAccess == ResourceBarrierAccess.RenderTarget ||
                    barrier.NewAccess == ResourceBarrierAccess.DepthStencilRead ||
                    barrier.NewAccess == ResourceBarrierAccess.DepthStencilWrite))
                {
                    if (renderPass.UninitializedTextures.Contains(barrier.ResourceIndex))
                    {
                        flags |= TextureBarrierFlags.Discard;
                    }
                }

                _textureBarriers.Add(new TextureBarrier(
                    ConvertEnumToSync(barrier.OldSync, barrier.OldQueueSpec), ConvertEnumToSync(barrier.NewSync, barrier.NewQueueSpec),
                    ConvertEnumToAccess(barrier.OldAccess, barrier.OldQueueSpec), ConvertEnumToAccess(barrier.NewAccess, barrier.NewQueueSpec),
                    ConvertEnumToLayout(barrier.OldAccess, barrier.OldQueueSpec), ConvertEnumToLayout(barrier.NewAccess, barrier.NewQueueSpec),
                    (ID3D12Resource*)resource,
                    new BarrierSubresourceRange(uint.MaxValue),
                    flags));
            }
        }

        private static BarrierSync ConvertEnumToSync(ResourceBarrierSync sync, BarrierQueueSpec queueSpec)
        {
            queueSpec = BarrierQueueSpec.None;
            return sync switch
            {
                ResourceBarrierSync.None => BarrierSync.None,
                ResourceBarrierSync.All => BarrierSync.All,
                ResourceBarrierSync.AllShading => BarrierSync.AllShading,
                ResourceBarrierSync.Draw => BarrierSync.Draw,
                ResourceBarrierSync.Indirect => BarrierSync.ExecuteIndirect,
                ResourceBarrierSync.IndexInput => BarrierSync.IndexInput,
                ResourceBarrierSync.DepthStencil => BarrierSync.DepthStencil,
                ResourceBarrierSync.RenderTarget => BarrierSync.RenderTarget,
                ResourceBarrierSync.PixelShading => BarrierSync.PixelShading,
                ResourceBarrierSync.VertexShading => BarrierSync.VertexShading,
                ResourceBarrierSync.ComputeShading => BarrierSync.ComputeShading,
                ResourceBarrierSync.Raytracing => BarrierSync.Raytracing,
                ResourceBarrierSync.Copy => BarrierSync.Copy,
                ResourceBarrierSync.Resolve => BarrierSync.Resolve,
                ResourceBarrierSync.BuildRTAcceleration => BarrierSync.BuildRaytracingAccelerationStructure,
                ResourceBarrierSync.CopyRTAcceleration => BarrierSync.CopyRaytracingAccelerationStructure,
                ResourceBarrierSync.ClearUnorderedAccess => BarrierSync.ClearUnorderedAccessView,
                ResourceBarrierSync.Split => BarrierSync.Split,
                _ => throw new NotImplementedException(),
            };
        }

        private static BarrierAccess ConvertEnumToAccess(ResourceBarrierAccess access, BarrierQueueSpec queueSpec)
        {
            queueSpec = BarrierQueueSpec.None;
            return access switch
            {
                ResourceBarrierAccess.Common => BarrierAccess.Common,
                ResourceBarrierAccess.None => BarrierAccess.NoAccess,
                ResourceBarrierAccess.VertexBuffer => BarrierAccess.VertexBuffer,
                ResourceBarrierAccess.IndexBuffer => BarrierAccess.IndexBuffer,
                ResourceBarrierAccess.ConstantBuffer => BarrierAccess.ConstantBuffer,
                ResourceBarrierAccess.ShaderResource => BarrierAccess.ShaderResource,
                ResourceBarrierAccess.UnorderedAccess => BarrierAccess.UnorderedAccess,
                ResourceBarrierAccess.RenderTarget => BarrierAccess.RenderTarget,
                ResourceBarrierAccess.DepthStencilRead => BarrierAccess.DepthStencilRead,
                ResourceBarrierAccess.DepthStencilWrite => BarrierAccess.DepthStencilWrite,
                ResourceBarrierAccess.Indirect => BarrierAccess.IndirectArgument,
                ResourceBarrierAccess.Predication => BarrierAccess.Predication,
                ResourceBarrierAccess.CopySource => BarrierAccess.CopySource,
                ResourceBarrierAccess.CopyDestination => BarrierAccess.CopyDest,
                ResourceBarrierAccess.ResolveSource => BarrierAccess.ResolveSource,
                ResourceBarrierAccess.ResolveDestination => BarrierAccess.ResolveDest,
                ResourceBarrierAccess.RTAccelerationRead => BarrierAccess.RaytracingAccelerationStructureRead,
                ResourceBarrierAccess.RTAccelerationWrite => BarrierAccess.RaytracingAccelerationStructureWrite,
                ResourceBarrierAccess.ShadingRateSource => BarrierAccess.ShadingRateSource,
                _ => throw new NotImplementedException(),
            };
        }

        private static BarrierLayout ConvertEnumToLayout(ResourceBarrierAccess access, BarrierQueueSpec queueSpec)
        {
            queueSpec = BarrierQueueSpec.None;
            BarrierLayout newLayout = BarrierLayout.Common;
            switch (queueSpec)
            {
                case BarrierQueueSpec.Direct:
                    {
                        newLayout = access switch
                        {
                            ResourceBarrierAccess.Common => BarrierLayout.DirectQueueCommon,
                            ResourceBarrierAccess.ShaderResource => BarrierLayout.DirectQueueShaderResource,
                            ResourceBarrierAccess.UnorderedAccess => BarrierLayout.DirectQueueUnorderedAccess,
                            ResourceBarrierAccess.CopySource => BarrierLayout.DirectQueueCopySource,
                            ResourceBarrierAccess.CopyDestination => BarrierLayout.DirectQueueCopyDest,
                            _ => BarrierLayout.Common
                        };

                        break;
                    }
                case BarrierQueueSpec.Compute:
                    {
                        newLayout = access switch
                        {
                            ResourceBarrierAccess.Common => BarrierLayout.ComputeQueueCommon,
                            ResourceBarrierAccess.ShaderResource => BarrierLayout.ComputeQueueShaderResource,
                            ResourceBarrierAccess.UnorderedAccess => BarrierLayout.ComputeQueueUnorderedAccess,
                            ResourceBarrierAccess.CopySource => BarrierLayout.ComputeQueueCopySource,
                            ResourceBarrierAccess.CopyDestination => BarrierLayout.ComputeQueueCopyDest,
                            _ => BarrierLayout.Common
                        };

                        break;
                    }
                case BarrierQueueSpec.Direct | BarrierQueueSpec.Compute:
                    {
                        newLayout = access switch
                        {
                            ResourceBarrierAccess.ShaderResource => BarrierLayout.DirectQueueGenericReadComputeQueueAccessible,
                            ResourceBarrierAccess.CopySource => BarrierLayout.DirectQueueGenericReadComputeQueueAccessible,
                            _ => BarrierLayout.Common
                        };

                        break;
                    }
            }

            if (newLayout == BarrierLayout.Common)
            {
                newLayout = access switch
                {
                    ResourceBarrierAccess.Common => BarrierLayout.Common,
                    ResourceBarrierAccess.None => BarrierLayout.Undefined,
                    ResourceBarrierAccess.ShaderResource => BarrierLayout.ShaderResource,
                    ResourceBarrierAccess.UnorderedAccess => BarrierLayout.UnorderedAccess,
                    ResourceBarrierAccess.RenderTarget => BarrierLayout.RenderTarget,
                    ResourceBarrierAccess.DepthStencilRead => BarrierLayout.DepthStencilRead,
                    ResourceBarrierAccess.DepthStencilWrite => BarrierLayout.DepthStencilWrite,
                    ResourceBarrierAccess.CopySource => BarrierLayout.CopySource,
                    ResourceBarrierAccess.CopyDestination => BarrierLayout.CopyDest,
                    ResourceBarrierAccess.ResolveSource => BarrierLayout.ResolveSource,
                    ResourceBarrierAccess.ResolveDestination => BarrierLayout.ResolveDest,
                    ResourceBarrierAccess.ShadingRateSource => BarrierLayout.ShadingRateSource,
                    _ => throw new NotImplementedException(),
                };
            }

            return newLayout;
        }
    }
}
