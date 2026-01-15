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
    public ref struct ComputeCommandBuffer
    {
        private readonly RenderPassErrorReporter _errorReporter;
        private readonly RenderPassStateData _stateData;
        private readonly CommandRecorder _recorder;
        private readonly SequentialLinearAllocator _intermediateAllocator;
        private readonly FrameGraphResources _resources;

        internal ComputeCommandBuffer(RenderPassErrorReporter errorReporter, RenderPassStateData stateData, CommandRecorder recorder, SequentialLinearAllocator intermediateAllocator, FrameGraphResources resources)
        {
            _errorReporter = errorReporter;
            _stateData = stateData;
            _recorder = recorder;
            _intermediateAllocator = intermediateAllocator;
            _resources = resources;
        }

        public unsafe void SetPipeline(RHIComputePipeline pipeline)
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

        public void Dispatch(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
        {
            _recorder.AddExecutionCommand(RecCommandType.Dispatch, new UCDispatch
            {
                ThreadGroupCountX = threadGroupCountX,
                ThreadGroupCountY = threadGroupCountY,
                ThreadGroupCountZ = threadGroupCountZ
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

            static bool ValidateCopySource(ref ComputeCommandBuffer cmd, FGTextureCopySource src, FGResourceUsage usage)
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

        #region Barriers
        public void Barrier(FrameGraphBuffer buffer, FGBarrierSync syncAfter, FGBarrierAccess accessAfter, ulong offset, ulong size)
        {

        }

        public void Barrier(FrameGraphTexture texture, FGBarrierSync syncAfter, FGBarrierAccess accessAfter, FGBarrierLayout layoutAfter, FGBarrierSubresource subresource)
        {

        }
        #endregion
    }
}
