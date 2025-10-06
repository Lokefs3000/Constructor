using Primary.Rendering.Raw;
using Primary.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Pass
{
    internal sealed class RPBufferManager : IDisposable
    {
        private int[] _pastBufferSizes;
        private int _pastBufferSizesHead;
        private float _timeSinceLastEntry;

        private int _pastBufferSizeCount;

        private int _currentBufferSize;

        private List<StorageBuffer> _activeBuffers;
        private int _currentBufferOffset;

        private readonly int _baseBitCountTrail;

        private bool _disposedValue;

        internal RPBufferManager()
        {
            _pastBufferSizes = new int[Constants.rRPBufferManagerHistorySize];
            _pastBufferSizesHead = 0;
            _timeSinceLastEntry = 0.0f;

            _pastBufferSizeCount = 0;

            _currentBufferSize = 0;

            _activeBuffers = new List<StorageBuffer>();
            _currentBufferOffset = 0;

            _baseBitCountTrail = BitOperations.TrailingZeroCount(Constants.rRPBufferManagerMinimumSize);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                for (int i = 0; i < _activeBuffers.Count; i++)
                {
                    _activeBuffers[i].Dispose();
                }
                _activeBuffers.Clear();

                _disposedValue = true;
            }
        }

        ~RPBufferManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void AnalyzeFrameBuffers()
        {
            _timeSinceLastEntry += Time.DeltaTime;
            if (_timeSinceLastEntry >= Constants.rRPBufferManagerHistoryTime)
            {
                _pastBufferSizes[_pastBufferSizesHead] = _currentBufferSize;
                _currentBufferSize = (_currentBufferSize + 1 == _pastBufferSizes.Length ? 0 : _currentBufferSize + 1);
                _timeSinceLastEntry = 0.0f;

                if (_pastBufferSizeCount < _pastBufferSizes.Length)
                    _pastBufferSizeCount++;
            }

            int averageBufferSize = 0;
            if (_pastBufferSizeCount >= _pastBufferSizes.Length)
            {
                long total = 0;

                int head = _pastBufferSizesHead;
                for (int i = 0; i < _pastBufferSizes.Length; i++)
                {
                    total += _pastBufferSizes[head];

                    if (head == 0)
                        head = _pastBufferSizes.Length - 1;
                    else
                        head -= 1;
                }

                averageBufferSize = (int)BitOperations.RoundUpToPowerOf2((uint)(int)(total / _pastBufferSizes.LongLength));
            }

            if (_currentBufferSize != averageBufferSize)
            {
                for (int i = 0; i < _activeBuffers.Count; i++)
                {
                    _activeBuffers[i].Dispose();
                }

                _activeBuffers.Clear();
                _currentBufferSize = averageBufferSize;
            }
            else if (_activeBuffers.Count > 1)
            {
                for (int i = 0; i < _activeBuffers.Count - 1; i++)
                {
                    _activeBuffers[i].Dispose();
                }

                _activeBuffers.RemoveRange(0, _activeBuffers.Count - 1);
            }
        }

        private void AllocateNewBuffer(int minimumSize)
        {
            if (_currentBufferSize < minimumSize)
            {
                while ((_currentBufferSize = (int)BitOperations.RoundUpToPowerOf2((uint)(_currentBufferSize + 1))) < minimumSize)
                {

                }
            }

            _activeBuffers.Add(new StorageBuffer(RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)_currentBufferSize,
                Stride = 0,

                Memory = RHI.MemoryUsage.Dynamic,
                Usage = RHI.BufferUsage.None,
                Mode = RHI.BufferMode.None,
                CpuAccessFlags = RHI.CPUAccessFlags.None
            }, nint.Zero)));

            _currentBufferOffset = 0;
        }

        internal PassBuffer<T> Rent<T>() where T : unmanaged
        {
            int sz = Unsafe.SizeOf<T>();
            int descriptorIndex = 0;

            if (sz < Constants.rRPBufferManagerMinimumSize)
            {
                sz = Constants.rRPBufferManagerMinimumSize;
                descriptorIndex = 0;
            }
            else
            {
                sz = (int)BitOperations.RoundUpToPowerOf2((uint)sz);
                if (sz > Constants.rRPBufferManagerMaximumSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(sz), sz, "Object to big to allocate with render pass buffers");
                }

                descriptorIndex = BitOperations.TrailingZeroCount(sz) - _baseBitCountTrail;
            }

            if (_currentBufferSize < sz + _currentBufferOffset)
                AllocateNewBuffer(sz);

            StorageBuffer buffer = _activeBuffers[_activeBuffers.Count - 1];
            int currentOffset = _currentBufferOffset;

            _currentBufferOffset += sz;
            return new PassBuffer<T>(buffer.Buffer, buffer.Descriptors[descriptorIndex]);
        }

        private readonly record struct StorageBuffer : IDisposable
        {
            internal readonly RHI.Buffer Buffer;
            internal readonly RHI.Descriptor[] Descriptors;

            internal StorageBuffer(RHI.Buffer buffer)
            {
                Buffer = buffer;

                int stepCount = BitOperations.TrailingZeroCount(Constants.rRPBufferManagerMaximumSize) -
                                BitOperations.TrailingZeroCount(Constants.rRPBufferManagerMinimumSize);

                Descriptors = new RHI.Descriptor[stepCount];
                for (int i = Constants.rRPBufferManagerMaximumSize; i < Constants.rRPBufferManagerMaximumSize; i <<= 1)
                {
                    Descriptors[i] = buffer.AllocateDescriptor(new RHI.BufferCBDescriptorDescription
                    {
                        ByteOffset = 0,
                        SizeInBytes = (uint)i,
                        Flags = RHI.DescriptorFlags.Dynamic
                    });
                }
            }

            public void Dispose()
            {
                Buffer.Dispose(); //auto descriptor cleanup
            }
        }
    }

    public ref struct PassBuffer<T> where T : unmanaged
    {
        private RHI.Buffer? _bufferSource;
        private RHI.Descriptor? _descriptor;

        private RasterCommandBuffer? _commandBuffer;

        private Span<T> _mapped;

        internal PassBuffer(RHI.Buffer source, RHI.Descriptor descriptor)
        {
            _bufferSource = source;
            _descriptor = descriptor;
            _mapped = Span<T>.Empty;
        }

        public Span<T> GetOrMap(RasterCommandBuffer commandBuffer)
        {
            if (_commandBuffer == null)
            {
                _commandBuffer = commandBuffer;
                _mapped = commandBuffer.Map<T>(_bufferSource!, RHI.MapIntent.Write);
            }

            return _mapped;
        }

        internal void EndIfMapActive()
        {
            if (_commandBuffer != null)
            {
                _commandBuffer.Unmap(_bufferSource!);

                _commandBuffer = null;
                _mapped = Span<T>.Empty;
            }
        }

        internal RHI.Buffer? BufferSource => _bufferSource;
        internal RHI.Descriptor? Descriptor => _descriptor;

        /// <summary>
        /// DO NOT INVOKE A READ INSTRUCTION AS IT SLOWS STUFF DOWN ALOT
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12resource-map#simple-usage-models"/>
        /// </summary>
        public Span<T> Mapped => _mapped;
    }
}
