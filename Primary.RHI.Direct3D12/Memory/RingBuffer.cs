using Primary.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RHI.Direct3D12.Memory
{
    internal class RingBuffer
    {
        private Queue<FrameTailAttribs> _completedFrames;

        private ulong _head;
        private ulong _tail;
        private ulong _maxSize;
        private ulong _usedSize;
        private ulong _currFrameSize;

        internal RingBuffer(ulong size)
        {
            _completedFrames = new Queue<FrameTailAttribs>();

            _head = 0;
            _tail = 0;
            _maxSize = size;
            _usedSize = 0;
            _currFrameSize = 0;
        }

        internal ulong Allocate(ulong size)
        {
            if (IsFull)
                return InvalidOffset;

            if (_tail >= _head)
            {
                if (_tail + size <= _maxSize)
                {
                    ulong offset = _tail;
                    _tail += size;
                    _usedSize += size;
                    _currFrameSize += size;
                    return offset;
                }
                else if (size <= _head)
                {
                    ulong addSize = (_maxSize - _tail) + size;
                    _usedSize += addSize;
                    _currFrameSize += addSize;
                    _tail = size;
                    return 0;
                }
            }
            else if (_tail + size <= _head)
            {
                ulong offset = _tail;
                _tail += size;
                _usedSize += size;
                _currFrameSize += size;
                return offset;
            }

            return InvalidOffset;
        }

        internal void FinishCurrentFrame(ulong fenceValue)
        {
            if (_currFrameSize == 0)
                return;

            _completedFrames.Enqueue(new FrameTailAttribs(fenceValue, _tail, _currFrameSize));
            _currFrameSize = 0;
        }

        internal void ReleaseCompletedFrames(ulong completedFenceValue)
        {
            while (_completedFrames.Count > 0 && _completedFrames.Peek().FenceValue <= completedFenceValue)
            {
                FrameTailAttribs oldestFrameTail = _completedFrames.Dequeue();
                ExceptionUtility.Assert(oldestFrameTail.Size <= _usedSize);

                _usedSize -= oldestFrameTail.Size;
                _head = oldestFrameTail.Offset;
            }
        }

        internal bool IsFull => _usedSize == _maxSize;
        internal bool IsEmpty => _usedSize == 0;

        internal ulong MaxSize => _maxSize;

        internal const ulong InvalidOffset = ulong.MaxValue;

        private readonly record struct FrameTailAttribs(ulong FenceValue, ulong Offset, ulong Size);
    }
}
