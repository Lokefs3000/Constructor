using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Common.Memory;
using Primary.Rendering.Assets;
using Primary.Rendering.Resources;
using Primary.RHI2;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Rendering.Recording
{
    public sealed class CommandRecorder : IDisposable
    {
        private readonly RenderPassManager _manager;

        private LinearResizingAllocator _allocator;
        private int _commandCount;

        private HashSet<FrameGraphResource> _resourceSet;

        private bool _disposedValue;

        internal CommandRecorder(RenderPassManager manager)
        {
            _manager = manager;

            _allocator = new LinearResizingAllocator(2048);
            _commandCount = 0;

            _resourceSet = new HashSet<FrameGraphResource>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _allocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ResetForNewRecording()
        {
            _allocator.Reset();
            _commandCount = 0;

            _resourceSet.Clear();
        }

        internal unsafe void AddCommand<T>(RecCommandType commandType, T command) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>() + Unsafe.SizeOf<RecCommandType>();
            nint ptr = _allocator.Allocate(size);

            Unsafe.WriteUnaligned(ptr.ToPointer(), commandType);
            Unsafe.WriteUnaligned((ptr + Unsafe.SizeOf<RecCommandType>()).ToPointer(), command);

            if (commandType != RecCommandType.Dummy)
                _commandCount++;

#if DEBUG
            AddValidationCommand();
#endif
        }

        internal unsafe void AddBlankCommand(RecCommandType commandType)
        {
            int size = Unsafe.SizeOf<RecCommandType>();
            nint ptr = _allocator.Allocate(size);

            *(RecCommandType*)ptr = commandType;

            if (commandType != RecCommandType.Dummy)
                _commandCount++;

#if DEBUG
            AddValidationCommand();
#endif
        }

        [Conditional("DEBUG")]
        private unsafe void AddValidationCommand()
        {
            nint ptr = _allocator.Allocate(4);
            *(uint*)ptr = s_commandValidationHeader;
        }

        internal void AddResourceToSet(FrameGraphResource resource) => _resourceSet.Add(resource);

        public nint GetPointerAtOffset(int offset)
        {
            Debug.Assert(offset <= _allocator.CurrentOffset);
            return _allocator.Pointer + offset;
        }

        public unsafe RecCommandType GetCommandTypeAtOffset(int offset)
        {
            Debug.Assert(offset + Unsafe.SizeOf<RecCommandType>() <= _allocator.CurrentOffset);
            return Unsafe.ReadUnaligned<RecCommandType>((_allocator.Pointer + offset).ToPointer());
        }

        public unsafe T GetCommandAtOffset<T>(int offset) where T : unmanaged
        {
            Debug.Assert(offset + Unsafe.SizeOf<RecCommandType>() <= _allocator.CurrentOffset);
            return Unsafe.ReadUnaligned<T>((_allocator.Pointer + offset).ToPointer());
        }

        [Conditional("DEBUG")]
        internal unsafe void ValidateCommandAtOffset(int offset)
        {
            Debug.Assert(offset + 4 <= _allocator.CurrentOffset);
            if (*(uint*)(_allocator.Pointer + offset) != s_commandValidationHeader)
            {
                uint header = *(uint*)(_allocator.Pointer + offset);
                throw new InvalidDataException($"Incorrect header present at offset: {offset} (hex: {header:x8}, text: {Encoding.UTF8.GetString(MemoryMarshal.Cast<uint, byte>(new ReadOnlySpan<uint>(ref header)))})");
            }
        }

        public int BufferSize => _allocator.CurrentOffset;
        public int TotalCommandCount => _commandCount;

        public IReadOnlySet<FrameGraphResource> UsedResources => _resourceSet;

        private const uint s_commandValidationHeader = 0x54444d43;
    }

    public enum RecCommandType : byte
    {
        Undefined = 0,

        Dummy,

        SetRenderTarget,
        SetDepthStencil,

        ClearRenderTarget,
        ClearDepthStencil,

        SetViewport,
        SetScissor,

        SetStencilReference,

        SetBuffer,
        SetProperties,
        SetConstants,

        UploadBuffer,
        UploadTexture,

        CopyBuffer,
        CopyTexture,

        DrawInstanced,
        DrawIndexedInstanced,

        Dispatch,

        SetPipeline,
        
        PresentOnWindow,

        //New,
        SetVertexBuffer,
        SetIndexBuffer,
        SetResource,
        SetRawData,
        SetResourcesInfo,

        CommitResources,
        CommitRenderTargets,
        CommitViewports,
        CommitScissors
    }

    public enum RecCommandContextType : byte
    {
        StateChange = 0,    //Changes the state (Viewports, Buffers, Outputs, etc)
        Execution,          //Flushes the state and executes a command (Draw, Dispatch, etc)
        Modification        //Modifies a resource (Clear, Copy, Barrier, etc)
    }

    public enum RecCommandEffectFlags : ushort
    {
        None = 0,

        ColorTarget = 1 << 0,
        DepthStencilTarget = 1 << 1,

        Viewport = 1 << 2,
        Scissor = 1 << 3,

        StencilRef = 1 << 4,

        VertexBuffer = 1 << 5,
        IndexBuffer = 1 << 6,

        Properties = 1 << 7,
        Constants = 1 << 9,

        Pipeline = 1 << 8
    }
}
