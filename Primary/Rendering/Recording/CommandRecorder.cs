using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using NativeCollections;
using Primary.Common.Memory;
using Primary.Pooling;
using Primary.Rendering.Commands;

namespace Primary.Rendering.Recording
{
    internal unsafe sealed class CommandRecorder : IDisposable
    {
        private readonly LinearResizingAllocator _allocator;

        private bool _disposedValue;

        internal CommandRecorder()
        {
            _allocator = new LinearResizingAllocator(2048);
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

        internal void ClearRecording()
        {
            _allocator.Reset();
        }

        internal void RecordCommand(CmdCommandType commandType)
        {
            *(CmdCommandType*)_allocator.Allocate(Unsafe.SizeOf<CmdCommandType>()) = commandType;
        }

        internal void RecordCommand<T>(CmdCommandType commandType, T commandData) where T : unmanaged
        {
            byte* allocated = (byte*)_allocator.Allocate(Unsafe.SizeOf<CmdCommandType>() + Unsafe.SizeOf<T>()).ToPointer();

            *(CmdCommandType*)allocated = commandType;
            *(T*)(allocated + Unsafe.SizeOf<CmdCommandType>()) = commandData;
        }

        internal void RecordCommandData<T>(T commandData) where T : unmanaged
        {
            *(T*)_allocator.Allocate(Unsafe.SizeOf<T>()) = commandData;
        }

        internal void RecordCommandData<T>(ReadOnlySpan<T> commandData) where T : unmanaged
        {
            if (!commandData.IsEmpty)
            {
                int byteSize = Unsafe.SizeOf<T>() * commandData.Length;
                T* allocated = (T*)_allocator.Allocate(byteSize);
                commandData.CopyTo(new Span<T>(allocated, byteSize));
            }
        }

        internal CmdCommandType ReadCommandType(int currentOffset) => *(CmdCommandType*)_allocator.GetAtOffset(currentOffset);
        internal T ReadCommandData<T>(int currentOffset) where T : unmanaged => *(T*)_allocator.GetAtOffset(currentOffset);
       
        internal T* GetCommandPointer<T>(int currentOffset) where T : unmanaged => (T*)_allocator.GetAtOffset(currentOffset);

        public int CommandOffset => _allocator.CurrentOffset;

        internal record class PoolingPolicy : IObjectPoolPolicy<CommandRecorder>
        {
            public CommandRecorder Create() => new CommandRecorder();

            public bool Return(ref CommandRecorder obj)
            {
                obj.ClearRecording();
                return true;
            }

            public static readonly PoolingPolicy Instance = new PoolingPolicy();
        }
    }
}
