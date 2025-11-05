using CommunityToolkit.HighPerformance;
using Primary.Common;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering
{
    public unsafe sealed class FrameUploadManager : IDisposable
    {
        private static WeakReference s_instance = new WeakReference(null);

        private readonly RHI.GraphicsDevice _graphicsDevice;

        private ConcurrentQueue<PendingUpload> _pendingUploads;
        private Queue<nint> _pendingFrees;

        private RHI.CopyCommandBuffer _commandBuffer;

        private bool _disposedValue;

        internal FrameUploadManager(RHI.GraphicsDevice gd)
        {
            s_instance.Target = this;

            _graphicsDevice = gd;

            _pendingUploads = new ConcurrentQueue<PendingUpload>();
            _pendingFrees = new Queue<nint>();

            _commandBuffer = gd.CreateCopyCommandBuffer();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    s_instance.Target = null;
                }

                

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

        public static void ScheduleUpload(RHI.Resource resource, nint dataPointer, uint dataSize, UploadDescription description)
        {
            if (resource is RHI.Texture)
            {
                throw new NotImplementedException();
            }

            FrameUploadManager uploadManager = NullableUtility.ThrowIfNull(Unsafe.As<FrameUploadManager>(s_instance.Target));

            PendingUpload upload = new PendingUpload { Resource = resource };
            switch (description.MemoryStrategy)
            {
                case UploadMemoryStrategy.Copy:
                    {
                        upload.DataPointer = (nint)NativeMemory.Alloc(dataSize);
                        upload.DataSize = dataSize;

                        NativeMemory.Copy(dataPointer.ToPointer(), upload.DataPointer.ToPointer(), dataSize);
                        break;
                    }
                case UploadMemoryStrategy.TransferOwnership:
                    {
                        upload.DataPointer = dataPointer;
                        upload.DataSize = dataSize;
                        break;
                    }
                default: throw new NotSupportedException();
            }
        }

        public static void ScheduleUpload<T>(RHI.Resource resource, Span<T> data, UploadDescription description) where T : unmanaged
            => ScheduleUpload(resource, (nint)Unsafe.AsPointer(ref data.DangerousGetReference()), (uint)(data.Length * Unsafe.SizeOf<T>()), description);

        //private const uint ScratchBufferSize = 16777216u; //16mb
        //private const int MaxActiveScratchBuffers = 4; //max 64mb

        private record struct PendingUpload
        {
            public RHI.Resource Resource;

            public nint DataPointer;
            public uint DataSize;
        }
    }

    public enum UploadScheduleTarget : byte
    {
        None = 0, //None:    Just upload whenever possible without time constraints
        Frame,    //Frame:   Upload before drawing starts
        Instant,  //Instant: Upload as soon as possible using the provided command buffer
    }

    public enum UploadMemoryStrategy : byte
    {
        /// <summary>Copy the memory into an internal buffer</summary>
        Copy = 0,
        /// <summary>Take ownership of memory (DANGER: MUST BE ALLOCATED WITH <see cref="NativeMemory"/>)</summary>
        TransferOwnership
    }

    public record struct UploadDescription
    {
        //public UploadScheduleTarget Target;
        //public RHI.CommandBuffer? CommandBuffer; //Required for "Instant" scheduling.
        public UploadMemoryStrategy MemoryStrategy;
        public Action? UploadFinishedCallback; //Only called with "None" scheduling.

        public UploadDescription()
        {
            //Target = UploadScheduleTarget.None;
            //CommandBuffer = null;
            MemoryStrategy = UploadMemoryStrategy.Copy;
            UploadFinishedCallback = null;
        }

        //public static explicit operator UploadDescription(UploadScheduleTarget target) => new UploadDescription(target);
    }
}
