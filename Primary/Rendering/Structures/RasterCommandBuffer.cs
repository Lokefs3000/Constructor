using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Memory;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI2;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Structures
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

            _recorder.AddStateChangeCommand(RecCommandType.SetRenderTarget, RecCommandEffectFlags.ColorTarget, new UCSetRenderTarget
            {
                Slot = (byte)slot,
                IsExternal = renderTarget.IsExternal,
                Texture = renderTarget.IsExternal ? (nint)Unsafe.As<RHITexture>(renderTarget.Resource!).GetAsNative() : renderTarget.Index,
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

        public unsafe void SetVertexBuffer(FGSetBufferDesc desc)
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.SetVertexBuffer, RPErrorType.NoShaderAccess, desc.Buffer.ToString());
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetBuffer, RecCommandEffectFlags.VertexBuffer, new UCSetBuffer
            {
                IsExternal = desc.Buffer.IsExternal,
                Buffer = desc.Buffer.IsExternal ? (nint)Unsafe.As<RHIBuffer>(desc.Buffer.Resource!).GetAsNative() : desc.Buffer.Index,
                Location = FGSetBufferLocation.VertexBuffer,
                Stride = desc.Stride == 0 ? (desc.Buffer.IsExternal ? Unsafe.As<RHIBuffer>(desc.Buffer.Resource!).Description.Stride : desc.Buffer.Description.Stride) : desc.Stride
            });
        }

        public unsafe void SetIndexBuffer(FGSetBufferDesc desc)
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.SetIndexBuffer, RPErrorType.NoShaderAccess, desc.Buffer.ToString());
                return;
            }

            _recorder.AddStateChangeCommand(RecCommandType.SetBuffer, RecCommandEffectFlags.IndexBuffer, new UCSetBuffer
            {
                IsExternal = desc.Buffer.IsExternal,
                Buffer = desc.Buffer.IsExternal ? (nint)Unsafe.As<RHIBuffer>(desc.Buffer.Resource!).GetAsNative() : desc.Buffer.Index,
                Location = FGSetBufferLocation.IndexBuffer,
                Stride = desc.Stride == 0 ? (desc.Buffer.IsExternal ? Unsafe.As<RHIBuffer>(desc.Buffer.Resource!).Description.Stride : desc.Buffer.Description.Stride) : desc.Stride
            });
        }

        public void SetPipeline(RHIGraphicsPipeline pipeline)
        {
            int index = _resources.AddPontentialPipeline(pipeline);

            _recorder.AddStateChangeCommand(RecCommandType.SetPipeline, RecCommandEffectFlags.Pipeline, new UCSetPipeline
            {
                Pipeline = index
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

        public unsafe void SetConstants<T>(T data) where T : unmanaged
        {
            if (Unsafe.SizeOf<T>() % 4 != 0)
            {
                _errorReporter.ReportError(RPErrorSource.SetConstants, RPErrorType.InvalidStride, null);
                return;
            }

            if (Unsafe.SizeOf<T>() > 128)
            {
                _errorReporter.ReportError(RPErrorSource.SetConstants, RPErrorType.OutOfRange, null);
                return;
            }

            nint ptr = _intermediateAllocator.Allocate(Unsafe.SizeOf<T>());
            *(T*)ptr.ToPointer() = data;

            _recorder.AddStateChangeCommand(RecCommandType.SetConstants, RecCommandEffectFlags.Constants, new UCSetConstants
            {
                ConstantsDataSize = Unsafe.SizeOf<T>(),
                DataPointer = ptr
            });
        }

        public unsafe void SetConstants(ReadOnlySpan<uint> data)
        {
            if (data.Length > 32)
            {
                _errorReporter.ReportError(RPErrorSource.SetConstants, RPErrorType.OutOfRange, null);
                return;
            }

            int dataSize = data.Length * 4;

            nint ptr = _intermediateAllocator.Allocate(dataSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(ptr.ToPointer()), ref Unsafe.As<uint, byte>(ref data.DangerousGetReference()), (uint)dataSize);

            _recorder.AddStateChangeCommand(RecCommandType.SetConstants, RecCommandEffectFlags.Constants, new UCSetConstants
            {
                ConstantsDataSize = dataSize,
                DataPointer = ptr
            });
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

        public unsafe void Upload<T>(FGBufferUploadDesc desc, ReadOnlySpan<T> data) where T : unmanaged
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Write))
            {
                _errorReporter.ReportError(RPErrorSource.UploadBuffer, RPErrorType.NoResourceAccess, desc.Buffer.ToString());
                return;
            }

            int totalDataSize = Unsafe.SizeOf<T>() * data.Length;
            if (desc.Buffer.Description.Width + desc.Offset < totalDataSize)
            {
                _errorReporter.ReportError(RPErrorSource.UploadBuffer, RPErrorType.ResourceTooSmall, desc.Buffer.ToString());
                return;
            }

            nint ptr = _intermediateAllocator.Allocate(totalDataSize);
            NativeMemory.Copy(Unsafe.AsPointer(in data.DangerousGetReference()), ptr.ToPointer(), (nuint)totalDataSize);

            int index = _resources.AddBufferUpload(desc.Buffer, 0, totalDataSize);

            _recorder.AddModificationCommand(RecCommandType.UploadBuffer, new UCUploadBuffer
            {
                BufferUploadIndex = index,
                DataPointer = ptr,
                DataSize = (uint)totalDataSize,
                BufferOffset = desc.Offset
            });
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

        public unsafe void Upload<T>(FGTextureUploadDesc desc, Span<T> data) where T : unmanaged
        {
            if (!desc.Texture.IsExternal && !_stateData.ContainsResource(desc.Texture, FGResourceUsage.Write))
            {
                _errorReporter.ReportError(RPErrorSource.UploadTexture, RPErrorType.NoResourceAccess, desc.Texture.ToString());
                return;
            }

            if (desc.SubresourceIndex > FGResourceUtility.GetMaxSubresources(desc.Texture))
            {
                _errorReporter.ReportError(RPErrorSource.UploadTexture, RPErrorType.InvalidSubresource, desc.Texture.ToString());
                return;
            }

            (int width, int height, int depth) = FGResourceUtility.GetTextureSize(desc.Texture);
            (width, height, depth) = FGResourceUtility.GetSizeForSubresource(desc.SubresourceIndex, width, height, depth);

            RHIFormat format = FGResourceUtility.GetFormat(desc.Texture);
            RHIFormatInfo fi = RHIFormatInfo.Query(format);

            FGBox dest = desc.DestinationBox.GetValueOrDefault(new FGBox(0, 0, 0, width, height, depth));

            int totalDataSize = dest.Width * dest.Height * dest.Depth * fi.BytesPerPixel;
            int alignedDataSize = (dest.Width + (-dest.Width & 255)) * dest.Height * dest.Depth * fi.BytesPerPixel;

            int providedRowPitch = desc.DataRowPitch == 0 ? dest.Width * fi.BytesPerPixel : desc.DataRowPitch;
            int providedDataSize = providedRowPitch * dest.Height * dest.Depth;

            if (providedDataSize > data.Length * Unsafe.SizeOf<T>())
            {
                _errorReporter.ReportError(RPErrorSource.UploadTexture, RPErrorType.NotEnoughDataSupplied, desc.Texture.ToString());
                return;
            }

            if (desc.DestinationBox.HasValue)
            {
                if (dest.X + dest.Width > width || dest.Y + dest.Height > height || dest.Z + dest.Depth > depth)
                {
                    _errorReporter.ReportError(RPErrorSource.UploadTexture, RPErrorType.OutOfRange, desc.Texture.ToString());
                    return;
                }
            }

            if (totalDataSize > providedDataSize)
            {
                _errorReporter.ReportError(RPErrorSource.UploadTexture, RPErrorType.NotEnoughDataSupplied, desc.Texture.ToString());
                return;
            }

            nint ptr = _intermediateAllocator.Allocate(providedDataSize);
            Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(ptr.ToPointer()), ref Unsafe.As<T, byte>(ref data.DangerousGetReference()), (uint)providedDataSize);

            int index = _resources.AddTextureUpload(desc.Texture, 0, alignedDataSize);

            _recorder.AddModificationCommand(RecCommandType.UploadTexture, new UCUploadTexture
            {
                TextureUploadIndex = index,
                DestinationBox = desc.DestinationBox,
                SubresourceIndex = desc.SubresourceIndex,
                DataPointer = ptr,
                DataSize = (uint)providedDataSize,
                DataRowPitch = (uint)providedRowPitch
            });
        }

        public unsafe void Copy(FGBufferCopyDesc desc)
        {
            if (!desc.Source.IsExternal && !_stateData.ContainsResource(desc.Source, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.CopyBuffer, RPErrorType.NoResourceAccess, desc.Source.ToString());
                return;
            }

            if (desc.SrcOffset + desc.NumBytes > FGResourceUtility.GetWidth(desc.Source))
            {
                _errorReporter.ReportError(RPErrorSource.CopyBuffer, RPErrorType.OutOfRange, desc.Source.ToString());
                return;
            }

            if (!desc.Destination.IsExternal && !_stateData.ContainsResource(desc.Destination, FGResourceUsage.Write))
            {
                _errorReporter.ReportError(RPErrorSource.CopyBuffer, RPErrorType.NoResourceAccess, desc.Destination.ToString());
                return;
            }

            if (desc.DstOffset + desc.NumBytes > FGResourceUtility.GetWidth(desc.Destination))
            {
                _errorReporter.ReportError(RPErrorSource.CopyBuffer, RPErrorType.OutOfRange, desc.Destination.ToString());
                return;
            }

            _recorder.AddModificationCommand(RecCommandType.CopyBuffer, new UCCopyBuffer
            {
                SourceIsExternal = desc.Source.IsExternal,
                SourceBuffer = desc.Source.IsExternal ? (nint)Unsafe.As<RHIBuffer>(desc.Source.Resource!).GetAsNative() : desc.Source.Index,
                SourceOffset = desc.SrcOffset,
                DestinationIsExternal = desc.Destination.IsExternal,
                DestinationBuffer = desc.Destination.IsExternal ? (nint)Unsafe.As<RHIBuffer>(desc.Destination.Resource!).GetAsNative() : desc.Destination.Index,
                DestinationOffset = desc.DstOffset,
                NumBytes = desc.NumBytes,
            });
        }

        public unsafe void Copy(FGTextureCopyDesc desc)
        {
            if (!ValidateCopySource(ref this, desc.Source, FGResourceUsage.Read))
                return;
            if (!ValidateCopySource(ref this, desc.Destination, FGResourceUsage.Write))
                return;

            if (desc.SourceBox.HasValue)
            {
                FGBox box = desc.SourceBox.Value;
                if (desc.Source.Type == FGTextureCopySourceType.SubresourceIndex)
                {
                    (int width, int height, int depth) = FGResourceUtility.GetTextureSize(desc.Source.Resource.AsTexture());
                    (width, height, depth) = FGResourceUtility.GetSizeForSubresource(desc.Source.SubresourceIndex, width, height, depth);

                    if (box.X + box.Width > width || box.Y + box.Height > height || box.Z + box.Depth > depth)
                    {
                        _errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, desc.Source.Resource.ToString());
                        return;
                    }
                }
                else
                {
                    if (box.X + box.Width > desc.Source.Footprint.Width || box.Y + box.Height > desc.Source.Footprint.Height || box.Z + box.Depth > desc.Source.Footprint.Depth)
                    {
                        _errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, desc.Source.Resource.ToString());
                        return;
                    }
                }
            }

            int srcWidth = -1;
            int srcHeight = -1;
            int srcDepth = -1;

            if (desc.SourceBox.HasValue)
            {
                FGBox box = desc.SourceBox.Value;
                if (desc.Source.Type == FGTextureCopySourceType.SubresourceIndex)
                {
                    (int width, int height, int depth) = FGResourceUtility.GetTextureSize(desc.Source.Resource.AsTexture());
                    (width, height, depth) = FGResourceUtility.GetSizeForSubresource(desc.Source.SubresourceIndex, width, height, depth);

                    if (box.X + box.Width > width || box.Y + box.Height > height || box.Z + box.Depth > depth)
                    {
                        _errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, desc.Source.Resource.ToString());
                        return;
                    }

                    srcWidth = width;
                    srcHeight = height;
                    srcDepth = depth;
                }
                else
                {
                    if (box.X + box.Width > desc.Source.Footprint.Width || box.Y + box.Height > desc.Source.Footprint.Height || box.Z + box.Depth > desc.Source.Footprint.Depth)
                    {
                        _errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, desc.Source.Resource.ToString());
                        return;
                    }

                    srcWidth = (int)desc.Source.Footprint.Width;
                    srcHeight = (int)desc.Source.Footprint.Height;
                    srcDepth = (int)desc.Source.Footprint.Depth;
                }
            }

            {
                if (desc.Destination.Type == FGTextureCopySourceType.SubresourceIndex)
                {
                    (int width, int height, int depth) = FGResourceUtility.GetTextureSize(desc.Destination.Resource.AsTexture());
                    (width, height, depth) = FGResourceUtility.GetSizeForSubresource(desc.Destination.SubresourceIndex, width, height, depth);

                    if (desc.SourceBox.HasValue)
                    {
                        FGBox box = desc.SourceBox.Value;
                        if (box.X + box.Width > width || box.Y + box.Height > height || box.Z + box.Depth > depth)
                        {
                            _errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, desc.Destination.Resource.ToString());
                            return;
                        }
                    }
                }
                else
                {
                    if (desc.SourceBox.HasValue)
                    {
                        FGBox box = desc.SourceBox.Value;
                        if (box.X + box.Width > desc.Destination.Footprint.Width || box.Y + box.Height > desc.Destination.Footprint.Height || box.Z + box.Depth > desc.Destination.Footprint.Depth)
                        {
                            _errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, desc.Destination.Resource.ToString());
                            return;
                        }
                    }
                }
            }

            UCCopyTexture.CopySource source = new UCCopyTexture.CopySource
            {
                IsExternal = desc.Source.Resource.IsExternal,
                Resource = desc.Source.Resource.IsExternal ? (nint)desc.Source.Resource.Resource!.GetBaseAsNative() : desc.Source.Resource.Index,
            };

            if (desc.Source.Type == FGTextureCopySourceType.SubresourceIndex)
            {
                source.Type = UCCopyTexture.CopySourceType.SubresourceIndex;
                source.SubresourceIndex = desc.Source.SubresourceIndex;
            }
            else
            {
                source.Type = UCCopyTexture.CopySourceType.Footprint;
                source.Footprint = new UCCopyTexture.Footprint
                {
                    Offset = desc.Source.Footprint.Offset,
                    Format = desc.Source.Footprint.Format,
                    Width = desc.Source.Footprint.Width,
                    Height = desc.Source.Footprint.Height,
                    Depth = desc.Source.Footprint.Depth,
                    RowPitch = desc.Source.Footprint.RowPitch
                };
            }

            UCCopyTexture.CopySource destination = new UCCopyTexture.CopySource
            {
                IsExternal = desc.Destination.Resource.IsExternal,
                Resource = desc.Destination.Resource.IsExternal ? (nint)desc.Destination.Resource.Resource!.GetBaseAsNative() : desc.Destination.Resource.Index,
            };

            if (desc.Destination.Type == FGTextureCopySourceType.SubresourceIndex)
            {
                destination.Type = UCCopyTexture.CopySourceType.SubresourceIndex;
                destination.SubresourceIndex = desc.Destination.SubresourceIndex;
            }
            else
            {
                destination.Type = UCCopyTexture.CopySourceType.Footprint;
                destination.Footprint = new UCCopyTexture.Footprint
                {
                    Offset = desc.Destination.Footprint.Offset,
                    Format = desc.Destination.Footprint.Format,
                    Width = desc.Destination.Footprint.Width,
                    Height = desc.Destination.Footprint.Height,
                    Depth = desc.Destination.Footprint.Depth,
                    RowPitch = desc.Destination.Footprint.RowPitch
                };
            }

            _recorder.AddModificationCommand(RecCommandType.CopyTexture, new UCCopyTexture
            {
                Source = source,
                SourceBox = desc.SourceBox,
                Destination = destination,
                DstX = desc.DstX,
                DstY = desc.DstY,
                DstZ = desc.DstZ,
            });

            static bool ValidateCopySource(ref RasterCommandBuffer cmd, FGTextureCopySource src, FGResourceUsage usage)
            {
                if (!src.Resource.IsExternal && !cmd._stateData.ContainsResource(src.Resource, usage))
                {
                    cmd._errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.NoResourceAccess, src.Resource.ToString());
                    return false;
                }

                if (src.Type == FGTextureCopySourceType.SubresourceIndex)
                {
                    if (src.Resource.ResourceId != FGResourceId.Texture)
                    {
                        cmd._errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.InvalidResourceType, src.Resource.ToString());
                        return false;
                    }

                    if (src.SubresourceIndex > FGResourceUtility.GetMaxSubresources(src.Resource))
                    {
                        cmd._errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.InvalidSubresource, src.Resource.ToString());
                        return false;
                    }
                }
                else
                {
                    FGTextureFootprint footprint = src.Footprint;
                    if (src.Resource.ResourceId != FGResourceId.Buffer)
                    {
                        cmd._errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.InvalidResourceType, src.Resource.ToString());
                        return false;
                    }

                    //TODO: add "footprint" format validation

                    if (src.Resource.ResourceId == FGResourceId.Buffer)
                    {
                        int totalByteSizeRequied = (int)(RHIFormatInfo.Query(footprint.Format).BytesPerPixel * footprint.Width * footprint.Height * footprint.Depth);
                        if (totalByteSizeRequied > FGResourceUtility.GetWidth(src.Resource.AsBuffer()))
                        {
                            cmd._errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, src.Resource.ToString());
                            return false;
                        }
                    }
                    else
                    {
                        int totalByteSizeRequied = (int)(RHIFormatInfo.Query(footprint.Format).BytesPerPixel * footprint.Width * footprint.Height * footprint.Depth);
                        if (totalByteSizeRequied > (int)(RHIFormatInfo.Query(FGResourceUtility.GetFormat(src.Resource.AsTexture())).BytesPerPixel * footprint.Width * footprint.Height * footprint.Depth))
                        {
                            cmd._errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, src.Resource.ToString());
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public unsafe FGMappedSubresource<T> Map<T>(FGMapBufferDesc desc) where T : unmanaged
        {
            FrameGraphResources resources = _resources;
            CommandRecorder recorder = _recorder;
            Action<nint, uint, FGMapBufferDesc> callback = (x, y, z) => Unmap(resources, recorder, x, y, z);

            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Write))
            {
                _errorReporter.ReportError(RPErrorSource.MapBuffer, RPErrorType.NoResourceAccess, desc.Buffer.ToString());
                return new FGMappedSubresource<T>(callback, desc, nint.Zero, Span<T>.Empty);
            }

            uint actualBufferSize = desc.Buffer.Description.Width;
            uint totalElementCount = (uint)(desc.ElementCount == 0 ? (actualBufferSize / Unsafe.SizeOf<T>()) : desc.ElementCount);
            uint totalElementSize = (uint)(totalElementCount * Unsafe.SizeOf<T>());

            if ((desc.ElementCount == 0 ? actualBufferSize : desc.ElementCount) + desc.Offset > actualBufferSize)
            {
                _errorReporter.ReportError(RPErrorSource.MapBuffer, RPErrorType.OutOfRange, desc.Buffer.ToString());
                return new FGMappedSubresource<T>(callback, desc, nint.Zero, Span<T>.Empty);
            }

            nint ptr = _intermediateAllocator.Allocate((int)totalElementSize);
            return new FGMappedSubresource<T>(callback, desc, ptr, new Span<T>(ptr.ToPointer(), (int)totalElementCount));
        }

        public unsafe FGMappedSubresource<T> Map<T>(FrameGraphTexture texture) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        internal static void Unmap(FrameGraphResources resources, CommandRecorder recorder, nint rawPtr, uint dataSize, FGMapBufferDesc desc)
        {
            int index = resources.AddBufferUpload(desc.Buffer, (int)desc.Offset, (int)dataSize);

            recorder.AddModificationCommand(RecCommandType.UploadBuffer, new UCUploadBuffer
            {
                BufferUploadIndex = index,
                DataPointer = rawPtr,
                DataSize = dataSize,
                BufferOffset = desc.Offset
            });
        }

        internal unsafe void PresentOnWindow(Window window, FrameGraphTexture texture)
        {
            if (!_stateData.ContainsResource(texture, FGResourceUsage.Read | FGResourceUsage.NoShaderAccess))
            {
                _errorReporter.ReportError(RPErrorSource.PresentOnWindow, RPErrorType.NoResourceAccess, texture.ToString());
                return;
            }

            _recorder.AddExecutionCommand(RecCommandType.PresentOnWindow, new UCPresentOnWindow
            {
                IsExternal = texture.IsExternal,
                Texture = texture.IsExternal ? (nint)Unsafe.As<RHITexture>(texture.Resource!).GetAsNative() : texture.Index,
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

    public readonly record struct FGTextureUploadDesc(FrameGraphTexture Texture, FGBox? DestinationBox, uint SubresourceIndex, int DataRowPitch)
    {
        public static implicit operator FGTextureUploadDesc(FrameGraphTexture texture) => new FGTextureUploadDesc(texture, null, 0, 0);
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

        public FGTextureCopySource(FrameGraphTexture Resource, uint SubresourceIndex)
        {
            this.Resource = Resource;
            this.Type = FGTextureCopySourceType.SubresourceIndex;
            _union = new __Union(SubresourceIndex);
        }

        public FGTextureCopySource(FrameGraphResource Resource, FGTextureFootprint Footprint)
        {
            this.Resource = Resource;
            this.Type = FGTextureCopySourceType.Footprint;
            _union = new __Union(Footprint);
        }

        public static implicit operator FGTextureCopySource(FrameGraphTexture texture) => new FGTextureCopySource(texture, 0);

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

    public readonly record struct FGTextureFootprint(uint Offset, RHIFormat Format, uint Width, uint Height, uint Depth, uint RowPitch);

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
        private readonly Action<nint, uint, FGMapBufferDesc> _unmapCallback;
        private readonly FGMapBufferDesc _desc;

        private readonly nint _raw;
        private readonly Span<T> _data;

        internal FGMappedSubresource(Action<nint, uint, FGMapBufferDesc> unmapCallback, FGMapBufferDesc desc, nint raw, Span<T> data)
        {
            _unmapCallback = unmapCallback;
            _desc = desc;
            _raw = raw;
            _data = data;
        }

        public void Dispose()
        {
            if (_raw != nint.Zero)
                _unmapCallback(_raw, (uint)(_data.Length * Unsafe.SizeOf<T>()), _desc);
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
