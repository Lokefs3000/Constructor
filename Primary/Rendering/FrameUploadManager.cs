using CommunityToolkit.HighPerformance;
using Primary.Common;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering
{
    public unsafe sealed class FrameUploadManager : IDisposable
    {
        private static FrameUploadManager? s_instance = null;

        private readonly RHI.GraphicsDevice _graphicsDevice;

        private ConcurrentQueue<PendingUpload> _pendingUploads;
        private List<PendingUpload> _tempUploadList;

        private Queue<nint> _pendingFrees;
        private Queue<RHI.Buffer> _pendingDisposes;

        private List<ScratchBuffer> _scratchBuffers;

        private RHI.CopyCommandBuffer _commandBuffer;

        private bool _disposedValue;

        internal FrameUploadManager(RHI.GraphicsDevice gd)
        {
            s_instance = this;

            _graphicsDevice = gd;

            _pendingUploads = new ConcurrentQueue<PendingUpload>();
            _tempUploadList = new List<PendingUpload>();

            _pendingFrees = new Queue<nint>();
            _pendingDisposes = new Queue<RHI.Buffer>();

            _scratchBuffers = new List<ScratchBuffer>();

            _commandBuffer = gd.CreateCopyCommandBuffer();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    s_instance = null;
                }

                FreePendingAllocations();

                _disposedValue = true;
            }
        }

        ~FrameUploadManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void FreePendingAllocations()
        {
            while (_pendingFrees.TryDequeue(out nint dataPointer))
            {
                NativeMemory.Free(dataPointer.ToPointer());
            }

            while (_pendingDisposes.TryDequeue(out RHI.Buffer? buffer))
            {
                buffer.Dispose();
            }
        }

        internal void UploadPending()
        {
            FreePendingAllocations();

            _commandBuffer.Begin();

            while (_pendingUploads.TryDequeue(out PendingUpload upload))
            {
                //TODO: use "Direct" instead of "Staged" for uploading staged and dynamic buffers
                if (!UploadResourceDirect(upload.Resource, upload.DataPointer, (uint)upload.DataSize, null))
                {
                    _tempUploadList.Add(upload);
                }
                else
                {
                    upload.Callback?.Invoke();

                    if (upload.OwnsMemory)
                        _pendingFrees.Enqueue(upload.DataPointer);
                }
            }

            _commandBuffer.End();
            _graphicsDevice.Submit(_commandBuffer);

            for (int i = 0; i < _tempUploadList.Count; i++)
            {
                _pendingUploads.Enqueue(_tempUploadList[i]);
            }

            _tempUploadList.Clear();
        }

        internal void OpenBuffersForFrame()
        {
            _commandBuffer.Begin();
        }

        internal void SubmitBuffersForEndOfFrame()
        {
            _commandBuffer.End();
            _graphicsDevice.Submit(_commandBuffer);
        }

        private RHI.Buffer AllocateUploadBuffer(uint capacity)
        {
            //TODO: add backup for out-of-memory issues but then again the RHI cannot handle them either
            return _graphicsDevice.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = capacity,
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Staging,
                Mode = RHI.BufferMode.None,
                Stride = 0,
                Usage = RHI.BufferUsage.None
            }, nint.Zero);
        }

        private SubAllocation AllocateFromScratchBuffer(uint requiredSize)
        {
            Span<ScratchBuffer> span = _scratchBuffers.AsSpan();
            for (int i = 0; i < _scratchBuffers.Count; i++)
            {
                ref ScratchBuffer buffer = ref span[i];
                if (buffer.UsedMemory + requiredSize < MaxActiveScratchBuffers)
                {
                    uint offset = buffer.UsedMemory;
                    buffer.UsedMemory += requiredSize;

                    return new SubAllocation
                    {
                        Buffer = buffer.Buffer,
                        Offset = offset,
                    };
                }
            }

            if (_scratchBuffers.Count < MaxActiveScratchBuffers)
            {
                RHI.Buffer buffer = AllocateUploadBuffer(ScratchBufferSize);

                _scratchBuffers.Add(new ScratchBuffer
                {
                    Buffer = buffer,
                    UsedMemory = requiredSize
                });

                return new SubAllocation
                {
                    Buffer = buffer,
                    Offset = 0,
                };
            }

            return SubAllocation.Empty;
        }

        #region Upload
        private bool UploadResourceDirect(RHI.Resource resource, nint dataPointer, uint dataSize, RHI.CommandBuffer? commandBuffer)
        {
            /*commandBuffer ??= _commandBuffer!;

            if (resource is RHI.Buffer buffer)
            {
                if (buffer.Description.Memory == RHI.MemoryUsage.Staging)
                {
                    nint mapped = commandBuffer.Map(resource, RHI.MapIntent.Write, null);
                    if (mapped == nint.Zero)
                    {
                        return false;
                    }
                    NativeMemory.Copy(dataPointer.ToPointer(), mapped.ToPointer(), dataSize);
                    commandBuffer.Unmap(resource, null);
                }
                else if (buffer.Description.Memory == RHI.MemoryUsage.Dynamic)
                {
                    nint mapped = commandBuffer.Map(resource, RHI.MapIntent.Write, null);
                    if (mapped == nint.Zero)
                    {
                        return false;
                    }
                    NativeMemory.Copy(dataPointer.ToPointer(), mapped.ToPointer(), dataSize);
                    commandBuffer.Unmap(resource, null);
                }
                else
                    return false;
            }*/

            return true;
        }

        private bool UploadResourceStaged(RHI.Resource resource, nint dataPointer, uint dataSize, RHI.CommandBuffer? commandBuffer, bool allowScratchBuffer = true)
        {
            /*commandBuffer ??= _commandBuffer!;

            //switch strategy to immediate alloc
            if (!allowScratchBuffer || dataSize > ScratchBufferSize)
            {
                RHI.Buffer tempBuffer = AllocateUploadBuffer(dataSize);

                nint mapped = commandBuffer.Map(resource, RHI.MapIntent.Write, null);
                if (mapped == nint.Zero)
                {
                    _pendingDisposes.Enqueue(tempBuffer);
                    throw new Exception("Map failed!");
                }
                NativeMemory.Copy(dataPointer.ToPointer(), mapped.ToPointer(), dataSize);
                commandBuffer.Unmap(resource, null);

                if (resource is RHI.Buffer buffer)
                    commandBuffer.CopyBufferRegion(tempBuffer, 0, buffer, 0, dataSize);

                //TODO: maybe use context to decide if deallocating is the ideal choice or to wait a few frames
                _pendingDisposes.Enqueue(tempBuffer);
            }
            else
            {
                SubAllocation allocation = AllocateFromScratchBuffer(dataSize);
                if (allocation.IsEmpty)
                {
                    return false;
                }

                nint mapped = commandBuffer.Map(allocation.Buffer!, RHI.MapIntent.Write, null);
                if (mapped == nint.Zero)
                {
                    throw new Exception("Map failed!");
                }
                NativeMemory.Copy(dataPointer.ToPointer(), ((nint)(mapped + allocation.Offset)).ToPointer(), dataSize);
                commandBuffer.Unmap(allocation.Buffer!, new RHI.MapRange(allocation.Offset, allocation.Offset + dataSize));

                if (resource is RHI.Buffer buffer)
                    commandBuffer.CopyBufferRegion(allocation.Buffer!, allocation.Offset, buffer, 0, dataSize);
            }*/

            return true;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScheduleUpload<T>(RHI.Buffer resource, Span<T> data, UploadDescription description) where T : unmanaged
            => ScheduleUpload(resource, (nint)Unsafe.AsPointer(ref data[0]), (uint)data.Length * (uint)sizeof(T), description);

        //TODO: add another option to copy memory instead of borrowing or taking
        //TODO: and turn "TakeOwnershipOfMemory" into an enum
        public static bool ScheduleUpload(RHI.Buffer resource, nint data, uint dataSize, UploadDescription description)
        {
            if (dataSize > resource.Description.ByteWidth || resource.Description.Memory == RHI.MemoryUsage.Immutable)
            {
                return false;
            }

            switch (description.Target)
            {
                case UploadScheduleTarget.None:
                    {
                        s_instance!._pendingUploads.Enqueue(new PendingUpload
                        {
                            Resource = resource,
                            DataPointer = data,
                            DataSize = dataSize,
                            OwnsMemory = description.TakeOwnershipOfMemory,
                            Callback = description.UploadFinishedCallback
                        });

                        break;
                    }
                case UploadScheduleTarget.Frame:
                    {
                        if ((resource.Description.Memory == RHI.MemoryUsage.Dynamic || resource.Description.Memory == RHI.MemoryUsage.Staging) && FlagUtility.HasFlag((int)resource.Description.CpuAccessFlags, (int)RHI.CPUAccessFlags.Write))
                        {
                            return s_instance!.UploadResourceDirect(resource, data, dataSize, null);
                        }
                        else
                        {
                            if (!s_instance!.UploadResourceStaged(resource, data, dataSize, null))
                                return s_instance!.UploadResourceStaged(resource, data, dataSize, null, false);
                            return true;
                        }
                    }
                case UploadScheduleTarget.Instant:
                    {
                        if (description.CommandBuffer == null)
                        {
                            return false;
                        }

                        if ((resource.Description.Memory == RHI.MemoryUsage.Dynamic || resource.Description.Memory == RHI.MemoryUsage.Staging) && FlagUtility.HasFlag((int)resource.Description.CpuAccessFlags, (int)RHI.CPUAccessFlags.Write))
                        {
                            return s_instance!.UploadResourceDirect(resource, data, dataSize, description.CommandBuffer);
                        }
                        else
                        {
                            if (!s_instance!.UploadResourceStaged(resource, data, dataSize, description.CommandBuffer))
                                return s_instance!.UploadResourceStaged(resource, data, dataSize, description.CommandBuffer, false);
                            return true;
                        }
                    }
            }

            return true;
        }

        private const uint ScratchBufferSize = 16777216u; //16mb
        private const int MaxActiveScratchBuffers = 4; //max 64mb

        private record struct PendingUpload
        {
            public RHI.Resource Resource;

            public nint DataPointer;
            public ulong DataSize;

            public bool OwnsMemory;
            public Action? Callback;
        }

        private record struct ScratchBuffer
        {
            public RHI.Buffer Buffer;
            public uint UsedMemory;
        }

        private record struct SubAllocation
        {
            public RHI.Buffer? Buffer;
            public uint Offset;

            public bool IsEmpty => Buffer == null && Offset == 0;

            public static readonly SubAllocation Empty = new SubAllocation { Buffer = null, Offset = 0 };
        }
    }

    public enum UploadScheduleTarget : byte
    {
        None = 0, //None:    Just upload whenever possible without time constraints
        Frame,    //Frame:   Upload before drawing starts
        Instant,  //Instant: Upload as soon as possible using the provided command buffer
    }

    public record struct UploadDescription
    {
        public UploadScheduleTarget Target;
        public RHI.CommandBuffer? CommandBuffer; //Required for "Instant" scheduling.
        public bool TakeOwnershipOfMemory; //DANGER: Only use if allocated with the "NativeMemory" class and is not referenced by outside code.
        public Action? UploadFinishedCallback; //Only called with "None" scheduling.

        public UploadDescription()
        {
            Target = UploadScheduleTarget.None;
            CommandBuffer = null;
            TakeOwnershipOfMemory = false;
            UploadFinishedCallback = null;
        }

        public UploadDescription(UploadScheduleTarget target)
        {
            Target = target;
            CommandBuffer = null;
            TakeOwnershipOfMemory = false;
            UploadFinishedCallback = null;
        }

        public static explicit operator UploadDescription(UploadScheduleTarget target) => new UploadDescription(target);
    }
}
