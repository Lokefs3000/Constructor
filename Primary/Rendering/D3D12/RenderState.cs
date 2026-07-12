using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.RHI.Direct3D12;
using Silk.NET.Direct3D12;
using Silk.NET.Maths;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Primary.Rendering.D3D12
{
    [SupportedOSPlatform("windows")]
    internal sealed class RenderState : IDisposable
    {
        public DynamicArray<CpuDescriptorHandle> RenderTargets;
        public CpuDescriptorHandle DepthStencil;

        public DynamicArray<Viewport> Viewports;
        public DynamicArray<Box2D<int>> Scissors;

        public D3D12RasterState RasterState;

        public ShHeaderFlags HeaderFlags;

        public DynamicPointer ResourceData;
        public int ResourceSize;

        public DynamicPointer ConstantsData;
        public int ConstantsSize;

        private bool _disposedValue;

        internal RenderState()
        {
            RenderTargets = new DynamicArray<CpuDescriptorHandle>(8);
            DepthStencil = new CpuDescriptorHandle();

            Viewports = new DynamicArray<Viewport>(8);
            Scissors = new DynamicArray<Box2D<int>>(8);

            RasterState = default;

            HeaderFlags = ShHeaderFlags.None;

            ResourceData = new DynamicPointer(0);
            ResourceSize = 0;

            ConstantsData = new DynamicPointer(128);
            ConstantsSize = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                ResourceData.Dispose();
                ConstantsData.Dispose();

                _disposedValue = true;
            }
        }

        ~RenderState()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Clear(NRDDevice device)
        {
            RenderTargets.Count = 0;
            DepthStencil = device.DSVDescriptorHeap.NullDescriptor;

            Viewports.Count = 0;
            Scissors.Count = 0;

            RasterState = default;

            HeaderFlags = ShHeaderFlags.None;

            ResourceSize = 0;
            ConstantsSize = 0;
        }

        internal void AllocateInternalBuffers(int resourceSize, int constantsSize)
        {
            ResourceData.EnsureMinimumSize(resourceSize);

            ResourceSize = resourceSize;
            ConstantsSize = constantsSize;
        }
    }

    internal struct DynamicArray<T>
    {
        private T[] _array;
        private int _count;

        internal DynamicArray(int count)
        {
            _array = new T[count];
            _count = 0;
        }

        public ref T this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < _array.Length);
                return ref _array.DangerousGetReferenceAt(index);
            }
        }

        public ref T DangerousGetReference() => ref _array.DangerousGetReference();

        public int Count { get => _count; set => _count = value; }
        public ReadOnlySpan<T> Span => _array.AsSpan();
    }

    internal unsafe struct DynamicPointer : IDisposable
    {
        private nint _pointer;
        private int _size;

        internal DynamicPointer(int startSize)
        {
            _pointer = startSize > 0 ? (nint)NativeMemory.Alloc((nuint)startSize) : nint.Zero;
            _size = startSize;
        }

        public void Dispose()
        {
            if (_pointer != nint.Zero)
                NativeMemory.Free(_pointer.ToPointer());

            _pointer = nint.Zero;
            _size = 0;
        }

        internal void EnsureMinimumSize(int minSize)
        {
            if (_size < minSize)
            {
                if (_pointer != nint.Zero)
                    NativeMemory.Free(_pointer.ToPointer());

                minSize = (int)BitOperations.RoundUpToPowerOf2((nuint)minSize);

                _pointer = (nint)NativeMemory.Alloc((nuint)minSize);
                _size = minSize;
            }
        }

        public void* ToPointer() => _pointer.ToPointer();

        public static nint operator+(DynamicPointer left, int right) => left._pointer + right;
    }
}
