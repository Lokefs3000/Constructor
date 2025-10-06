using Primary.Common;
using Primary.Rendering.Raw;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Rendering.Collections
{
    [DebuggerTypeProxy(typeof(GpuListDebuggerTypeProxy<>))]
    internal unsafe class GpuList<T> : IDisposable where T : unmanaged
    {
        private SafePtrArray<T> _array;
        private SafePtrArray<OccupancyType> _occupancyArray;

        private int _capacity;
        private int _count;

        private bool _wasModified;

        private int _minCapacity;
        private int _dataMax;
        private int _bufferCapacity;

        private float _oversizeShrinkPercentage;

        private RHI.Buffer _buffer;

        private bool _disposedValue;

        internal GpuList(int minCapacity = 8, float oversizeShrinkPercentage = 3.0f)
        {
            minCapacity = Math.Max((int)BitOperations.RoundUpToPowerOf2((uint)minCapacity), 1);

            _array = SafePtrArray<T>.Allocate(minCapacity);
            _occupancyArray = SafePtrArray<OccupancyType>.Allocate(minCapacity);

            _capacity = minCapacity;
            _count = 0;

            _wasModified = false;

            _minCapacity = minCapacity;
            _dataMax = 0;
            _bufferCapacity = minCapacity;

            _oversizeShrinkPercentage = oversizeShrinkPercentage;

            _buffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)(sizeof(T) * minCapacity),
                Stride = (uint)sizeof(T),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.Structured,
                Usage = RHI.BufferUsage.ShaderResource
            }, nint.Zero);

            _buffer.Name = $"GpuList<{typeof(T).Name}>";

            SafePtrArray<OccupancyType>.Fill(_occupancyArray, -1, (byte)OccupancyType.Empty);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _buffer?.Dispose();
                }

                _array.Dispose();
                _occupancyArray.Dispose();

                _capacity = 0;
                _count = 0;
                _minCapacity = 0;
                _dataMax = 0;

                _disposedValue = true;
            }
        }

        ~GpuList()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            _count = 0;
            _dataMax = 0;
        }

        public void Add(T value)
        {
            if (_dataMax == _capacity)
                ResizeCpuArray((int)BitOperations.RoundUpToPowerOf2((uint)(_dataMax + 1)));

            _array[_dataMax] = value;
            _occupancyArray[_dataMax] = OccupancyType.New;

            _dataMax++;
            _count++;
            _wasModified = true;
        }

        public void Remove(int index)
        {
            ExceptionUtility.Assert(index >= 0 && index < _dataMax);

            if (index == _dataMax)
                _dataMax--;

            _occupancyArray[index] = OccupancyType.Removed;

            _count--;
            _wasModified = true;
        }

        public void Replace(int index, T value)
        {
            ExceptionUtility.Assert(index >= 0 && index < _dataMax);
            ExceptionUtility.Assert(_occupancyArray[index] >= OccupancyType.Occupied);

            _array[index] = value;
            _occupancyArray[index] = OccupancyType.New;

            _wasModified = true;
        }

        public int FindIndex(Predicate<T> comparison)
        {
            for (int i = 0; i < _dataMax; i++)
            {
                if (_occupancyArray[i] >= OccupancyType.Occupied)
                {
                    if (comparison(_array[i]))
                        return i;
                }
            }

            return -1;
        }

        public int FindIndex(RefPredicate<T> comparison)
        {
            for (int i = 0; i < _dataMax; i++)
            {
                if (_occupancyArray[i] >= OccupancyType.Occupied)
                {
                    if (comparison(ref _array[i]))
                        return i;
                }
            }

            return -1;
        }

        public int GetRelativeIndex(int absoluteIndex)
        {
            Debug.Assert(absoluteIndex >= 0 && absoluteIndex < _dataMax);

            int j = 0;
            for (int i = 0; i < absoluteIndex; i++)
            {
                if (_occupancyArray[i] >= OccupancyType.Occupied)
                {
                    j++;
                }
            }

            Debug.Assert(j < _count);
            return j;
        }

        public void Flush(RHI.CommandBuffer commandBuffer)
        {
            if (_wasModified)
            {
                _wasModified = false;

                int roundedCount = Math.Max((int)BitOperations.RoundUpToPowerOf2((uint)_count), _minCapacity);
                if (roundedCount > _bufferCapacity || roundedCount / (float)_bufferCapacity > _oversizeShrinkPercentage)
                {
                    ResizeCpuArray(roundedCount);
                    ResizeGpuBuffer(roundedCount);

                    T* mappedPointer = (T*)commandBuffer.Map(_buffer, RHI.MapIntent.Write, (ulong)(_count * sizeof(T)));
                    if (mappedPointer == null)
                    {
                        return;
                    }

                    int j = 0;
                    for (int i = 0; i < _dataMax; i++)
                    {
                        if (_occupancyArray[i] >= OccupancyType.Occupied)
                        {
                            mappedPointer[j] = _array[i];
                            _array[j++] = _array[i];
                        }
                    }

                    commandBuffer.Unmap(_buffer);

                    SafePtrArray<OccupancyType>.Fill(_occupancyArray, _count, (byte)OccupancyType.Occupied);

                    _dataMax = _count;
                }
                else
                {
                    if (_bufferCapacity != _capacity)
                        ResizeCpuArray(_bufferCapacity);

                    T* mappedPointer = (T*)commandBuffer.Map(_buffer, RHI.MapIntent.Write, (ulong)(_count * sizeof(T)));
                    if (mappedPointer == null)
                    {
                        return;
                    }

                    int j = 0;
                    for (int i = 0; i < _dataMax; i++)
                    {
                        if (_occupancyArray[i] == OccupancyType.New)
                        {
                            mappedPointer[j++] = _array[i];
                        }
                    }

                    commandBuffer.Unmap(_buffer);

                    int idxStart = -1;
                    int idxEnd = -1;

                    for (int i = 0; i < _dataMax; i++)
                    {
                        switch (_occupancyArray[i])
                        {
                            case OccupancyType.Removed:
                                {
                                    if (idxStart != -1 && idxEnd != -1)
                                    {
                                        int length = idxStart - i;

                                        commandBuffer.CopyBufferRegion(_buffer, (uint)(idxEnd * sizeof(T)), _buffer, (uint)(idxStart * sizeof(T)), (uint)(length * sizeof(T)));
                                        SafePtrArray<T>.Copy(_array, idxStart, _array, idxEnd, length);

                                        i = idxEnd - 1;
                                    }

                                    idxStart = i;
                                    break;
                                }
                            case OccupancyType.New:
                            case OccupancyType.Occupied:
                                {
                                    if (idxStart != -1 && idxEnd == -1)
                                    {
                                        idxEnd = i;
                                    }

                                    break;
                                }
                        }
                    }

                    SafePtrArray<OccupancyType>.Fill(_occupancyArray, _count, (byte)OccupancyType.Occupied);

                    _dataMax = _count;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush(CommandBuffer commandBuffer) => Flush(commandBuffer.Wrapped);

        private void ResizeCpuArray(int newCapacity)
        {
            SafePtrArray<T> newArray = SafePtrArray<T>.Allocate(newCapacity);
            SafePtrArray<OccupancyType> newOccupancyArray = SafePtrArray<OccupancyType>.Allocate(newCapacity);

            SafePtrArray<T>.Copy(_array, newArray);
            SafePtrArray<OccupancyType>.Copy(_occupancyArray, newOccupancyArray);

            _array.Dispose();
            _occupancyArray.Dispose();

            _array = newArray;
            _occupancyArray = newOccupancyArray;

            _capacity = newCapacity;
        }

        private void ResizeGpuBuffer(int newCapacity)
        {
            _buffer?.Dispose();
            _buffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)(newCapacity * sizeof(T)),
                Stride = (uint)sizeof(T),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.Structured,
                Usage = RHI.BufferUsage.ShaderResource
            }, nint.Zero);
        }

        public T[] ToArray()
        {
            T[] array = new T[_count];

            int j = 0;
            for (int i = 0; i < _dataMax; i++)
            {
                if (_occupancyArray[i] >= OccupancyType.Occupied)
                {
                    array[j++] = _array[i];
                }
            }

            return array;
        }

        public int Count => _count;
        public int Capacity => _capacity;

        public RHI.Buffer GpuBuffer => _buffer;

        private enum OccupancyType : byte
        {
            Empty = 0,
            Removed,
            Occupied,
            New
        }
    }

    internal delegate bool RefPredicate<T>(ref readonly T obj);

    internal class GpuListDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly GpuList<T> _list;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return _list.ToArray();
            }
        }

        public GpuListDebuggerTypeProxy(GpuList<T> list) => _list = list;
    }
}
