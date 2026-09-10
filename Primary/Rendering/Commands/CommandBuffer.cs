using CommunityToolkit.HighPerformance;
using Primary.Common.Memory;
using Primary.Rendering.Assets;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.State;
using Primary.Rendering.Structures;
using Primary.RHI;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Mathematics;

namespace Primary.Rendering.Commands
{
    internal class CommandBuffer
    {
        protected readonly RenderPassErrorReporter _errorReporter;
        protected readonly RenderPassStateData _stateData;
        protected readonly CommandRecorder _recorder;
        protected readonly LinearBlockAllocator _intermediateAllocator;
        protected readonly FrameGraphResources _resources;
        protected readonly RenderState _state;

        public CommandBuffer(RenderPassErrorReporter errorReporter, RenderPassStateData stateData, CommandRecorder recorder, LinearBlockAllocator intermediateAllocator, FrameGraphResources resources, RenderState state)
        {
            _errorReporter = errorReporter;
            _stateData = stateData;
            _recorder = recorder;
            _intermediateAllocator = intermediateAllocator;
            _resources = resources;
            _state = state;
        }

        public void SetProperties(PropertyBlock block)
        {
            //if (block.IsOutOfDate)
            //    block.Reload();

            _state.SetPropertyBlock(block);
        }

        public void SetProperties(ROPropertyBlock block)
        {
            if (!block.IsNull)
                _state.SetPropertyBlock(block.InternalBlock!);
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

            _state.SetConstants(MemoryMarshal.Cast<T, uint>(new Span<T>(ref data)));
        }

        public unsafe void SetConstants(ReadOnlySpan<uint> data)
        {
            if (data.Length > 32)
            {
                _errorReporter.ReportError(RPErrorSource.SetConstants, RPErrorType.OutOfRange, null);
                return;
            }

            _state.SetConstants(data);
        }

        public unsafe void Upload<T>(FGBufferUploadDesc desc, ReadOnlySpan<T> data) where T : unmanaged
        {
            if (!desc.Buffer.IsExternal && !_stateData.ContainsResource(desc.Buffer, FGResourceUsage.Write))
            {
                _errorReporter.ReportError(RPErrorSource.UploadBuffer, RPErrorType.NoResourceAccess, desc.Buffer.ToString());
                return;
            }

            int totalDataSize = Unsafe.SizeOf<T>() * data.Length;
            if (totalDataSize + desc.Offset > FGResourceUtility.GetWidth(desc.Buffer))
            {
                _errorReporter.ReportError(RPErrorSource.UploadBuffer, RPErrorType.ResourceTooSmall, desc.Buffer.ToString());
                return;
            }

            nint ptr = _intermediateAllocator.Allocate(totalDataSize);
            NativeMemory.Copy(Unsafe.AsPointer(in data.DangerousGetReference()), ptr.ToPointer(), (nuint)totalDataSize);

            int index = _resources.AddBufferUpload(desc.Buffer, 0, totalDataSize);

            _recorder.AddCommand(RecCommandType.UploadBuffer, new CmdUploadBuffer
            {
                UploadIndex = index,

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

            _recorder.AddCommand(RecCommandType.UploadBuffer, new CmdUploadBuffer
            {
                UploadIndex = index,

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
                if (dest.Width == 0 || dest.Height == 0 || dest.Depth == 0)
                {
                    _errorReporter.ReportError(RPErrorSource.UploadTexture, RPErrorType.InvalidSize, desc.Texture.ToString());
                    return;
                }

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

            _recorder.AddCommand(RecCommandType.UploadTexture, new CmdUploadTexture
            {
                UploadIndex = index,

                DataRowPitch = (uint)providedRowPitch,
                DataSize = (uint)providedDataSize,
                DataPointer = ptr,

                Box = desc.DestinationBox,
                SubresourceIndex = desc.SubresourceIndex
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

            _recorder.AddCommand(RecCommandType.CopyBuffer, new CmdCopyBuffer
            {
                Source = desc.Source,
                SourceOffset = desc.SrcOffset,

                Destination = desc.Destination,
                DestinationOffset = desc.DstOffset,

                NumBytes = desc.NumBytes
            });
        }

        public unsafe void Copy(FGTextureCopyDesc desc)
        {
            if (!ValidateCopySource(this, desc.Source, FGResourceUsage.Read))
                return;
            if (!ValidateCopySource(this, desc.Destination, FGResourceUsage.Write))
                return;

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
            else
            {
                if (desc.Source.Type == FGTextureCopySourceType.SubresourceIndex)
                {
                    (int width, int height, int depth) = FGResourceUtility.GetTextureSize(desc.Source.Resource.AsTexture());
                    (width, height, depth) = FGResourceUtility.GetSizeForSubresource(desc.Source.SubresourceIndex, width, height, depth);

                    srcWidth = width;
                    srcHeight = height;
                    srcDepth = depth;
                }
                else
                {
                    srcWidth = (int)desc.Source.Footprint.Width;
                    srcHeight = (int)desc.Source.Footprint.Height;
                    srcDepth = (int)desc.Source.Footprint.Depth;
                }
            }

            if (srcWidth == 0 || srcHeight == 0 || srcDepth == 0)
                return;

            {
                if (desc.Destination.Type == FGTextureCopySourceType.SubresourceIndex)
                {
                    (int width, int height, int depth) = FGResourceUtility.GetTextureSize(desc.Destination.Resource.AsTexture());
                    (width, height, depth) = FGResourceUtility.GetSizeForSubresource(desc.Destination.SubresourceIndex, width, height, depth);

                    if (width - desc.DstX < srcWidth || height - desc.DstY < srcHeight || depth - desc.DstZ < srcDepth)
                    {
                        _errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, desc.Destination.Resource.ToString());
                        return;
                    }
                }
                else
                {
                    if (desc.Destination.Footprint.Width - desc.DstX < srcWidth || desc.Destination.Footprint.Height - desc.DstY < srcHeight || desc.Destination.Footprint.Depth - desc.DstZ < srcDepth)
                    {
                        _errorReporter.ReportError(RPErrorSource.CopyTexture, RPErrorType.OutOfRange, desc.Destination.Resource.ToString());
                        return;
                    }
                }
            }

            CmdDataTextureSource source = new CmdDataTextureSource
            {
                Resource = desc.Source.Resource
            };

            if (desc.Source.Type == FGTextureCopySourceType.SubresourceIndex)
            {
                source.Type = CmdDataTextureSourceType.SubresourceIndex;
                source.SubresourceIndex = desc.Source.SubresourceIndex;
            }
            else
            {
                source.Type = CmdDataTextureSourceType.Footprint;
                source.Footprint = new CmdDataTextureFootprint
                {
                    Offset = desc.Source.Footprint.Offset,

                    Format = desc.Source.Footprint.Format,

                    Width = desc.Source.Footprint.Width,
                    Height = desc.Source.Footprint.Height,
                    Depth = desc.Source.Footprint.Depth,

                    RowPitch = desc.Source.Footprint.RowPitch
                };
            }

            CmdDataTextureSource destination = new CmdDataTextureSource
            {
                Resource = desc.Destination.Resource,
            };

            if (desc.Destination.Type == FGTextureCopySourceType.SubresourceIndex)
            {
                destination.Type = CmdDataTextureSourceType.SubresourceIndex;
                destination.SubresourceIndex = desc.Destination.SubresourceIndex;
            }
            else
            {
                destination.Type = CmdDataTextureSourceType.Footprint;
                destination.Footprint = new CmdDataTextureFootprint
                {
                    Offset = desc.Destination.Footprint.Offset,

                    Format = desc.Destination.Footprint.Format,

                    Width = desc.Destination.Footprint.Width,
                    Height = desc.Destination.Footprint.Height,
                    Depth = desc.Destination.Footprint.Depth,

                    RowPitch = desc.Destination.Footprint.RowPitch
                };
            }

            _recorder.AddCommand(RecCommandType.CopyTexture, new CmdCopyTexture
            {
                Source = source,
                SourceBox = desc.SourceBox,

                Destination = destination,
                DstX = desc.DstX,
                DstY = desc.DstY,
                DstZ = desc.DstZ,
            });

            static bool ValidateCopySource(CommandBuffer cmd, FGTextureCopySource src, FGResourceUsage usage)
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

        public unsafe void Read(FGReadBufferDesc desc)
        {
            if (!desc.Source.IsExternal && !_stateData.ContainsResource(desc.Source, FGResourceUsage.Read))
            {
                _errorReporter.ReportError(RPErrorSource.ReadBuffer, RPErrorType.NoResourceAccess, desc.Source.ToString());
                return;
            }

            if (desc.SrcOffset + desc.NumBytes > FGResourceUtility.GetWidth(desc.Source))
            {
                _errorReporter.ReportError(RPErrorSource.ReadBuffer, RPErrorType.OutOfRange, desc.Source.ToString());
                return;
            }

            if (desc.DstOffset + desc.NumBytes > desc.Destination.Description.Width)
            {
                _errorReporter.ReportError(RPErrorSource.ReadBuffer, RPErrorType.OutOfRange, desc.Destination.ToString());
                return;
            }

            _recorder.AddCommand(RecCommandType.CopyBuffer, new CmdCopyBuffer
            {
                Source = desc.Source,
                SourceOffset = desc.SrcOffset,

                Destination = desc.Destination,
                DestinationOffset = desc.DstOffset,

                NumBytes = desc.NumBytes
            });
        }

        public unsafe void Read(FGReadTextureDesc desc)
        {
            if (!ValidateCopySource(this, desc.Source, FGResourceUsage.Read))
                return;

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
            else
            {
                if (desc.Source.Type == FGTextureCopySourceType.SubresourceIndex)
                {
                    (int width, int height, int depth) = FGResourceUtility.GetTextureSize(desc.Source.Resource.AsTexture());
                    (width, height, depth) = FGResourceUtility.GetSizeForSubresource(desc.Source.SubresourceIndex, width, height, depth);

                    srcWidth = width;
                    srcHeight = height;
                    srcDepth = depth;
                }
                else
                {
                    srcWidth = (int)desc.Source.Footprint.Width;
                    srcHeight = (int)desc.Source.Footprint.Height;
                    srcDepth = (int)desc.Source.Footprint.Depth;
                }
            }

            if (srcWidth == 0 || srcHeight == 0 || srcDepth == 0)
                return;

            CmdDataTextureSource source = new CmdDataTextureSource
            {
                Resource = desc.Source.Resource
            };

            if (desc.Source.Type == FGTextureCopySourceType.SubresourceIndex)
            {
                source.Type = CmdDataTextureSourceType.SubresourceIndex;
                source.SubresourceIndex = desc.Source.SubresourceIndex;
            }
            else
            {
                source.Type = CmdDataTextureSourceType.Footprint;
                source.Footprint = new CmdDataTextureFootprint
                {
                    Offset = desc.Source.Footprint.Offset,

                    Format = desc.Source.Footprint.Format,

                    Width = desc.Source.Footprint.Width,
                    Height = desc.Source.Footprint.Height,
                    Depth = desc.Source.Footprint.Depth,

                    RowPitch = desc.Source.Footprint.RowPitch
                };
            }

            CmdDataTextureSource destination = new CmdDataTextureSource
            {
                Resource = desc.Destination,
                Type = CmdDataTextureSourceType.SubresourceIndex,
                SubresourceIndex = 0
            };

            _recorder.AddCommand(RecCommandType.CopyTexture, new CmdCopyTexture
            {
                Source = source,
                SourceBox = desc.SourceBox,

                Destination = destination,
                DstX = desc.DstX,
                DstY = desc.DstY,
                DstZ = desc.DstZ,
            });

            static bool ValidateCopySource(CommandBuffer cmd, FGTextureCopySource src, FGResourceUsage usage)
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

            _recorder.AddCommand(RecCommandType.UploadBuffer, new CmdUploadBuffer
            {
                UploadIndex = index,

                DataPointer = rawPtr,
                DataSize = dataSize,

                BufferOffset = desc.Offset
            });
        }

        public CommandEventScope BeginEvent(ReadOnlySpan<byte> name, uint? color = null)
        {
            int textLength = Math.Min(name.Length, 127);

            _recorder.AddCommand(RecCommandType.BeginEvent, new CmdBeginEvent
            {
                Color = color ?? (uint)(name.GetDjb2HashCode() | 0xff000000),
                TextLength = (byte)textLength
            });

            _recorder.AddString(name[..textLength]);

            return new CommandEventScope(this);
        }

        internal void EndEvent()
        {
            _recorder.AddBlankCommand(RecCommandType.EndEvent);
        }

        public void MarkEvent(ReadOnlySpan<byte> name, uint? color = null)
        {
            int textLength = Math.Min(name.Length, 127);

            _recorder.AddCommand(RecCommandType.MarkEvent, new CmdBeginEvent
            {
                Color = color ?? (uint)(name.GetDjb2HashCode() | 0xff000000),
                TextLength = (byte)textLength
            });

            _recorder.AddString(name[..textLength]);
        }
    }

    public readonly record struct CommandEventScope : IDisposable
    {
        private readonly CommandBuffer _buffer;

        internal CommandEventScope(CommandBuffer buffer)
        {
            _buffer = buffer;
        }

        public void Dispose()
        {
            _buffer.EndEvent();
        }
    }

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

    public readonly record struct FGReadBufferDesc(FrameGraphBuffer Source, uint SrcOffset, RHIReadback Destination, uint DstOffset, uint NumBytes);
    public readonly record struct FGReadTextureDesc(FGTextureCopySource Source, FGBox? SourceBox, RHIReadback Destination, uint DstOffset, uint DstX, uint DstY, uint DstZ);

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
        private readonly CommandBuffer _owner;
        private readonly FGMapBufferDesc _desc;

        private readonly nint _raw;
        private readonly Span<T> _data;

        internal FGMappedSubresource(CommandBuffer owner, FGMapBufferDesc desc, nint raw, Span<T> data)
        {
            _owner = owner;
            _desc = desc;
            _raw = raw;
            _data = data;
        }

        public void Dispose()
        {
            if (_raw != nint.Zero)
                _owner.Unmap(_raw, (uint)(_data.Length * Unsafe.SizeOf<T>()), _desc);
        }

        public Span<T> Span => _data;
    }

    public enum FGTextureCopySourceType : byte
    {
        SubresourceIndex = 0,
        Footprint
    }
}
