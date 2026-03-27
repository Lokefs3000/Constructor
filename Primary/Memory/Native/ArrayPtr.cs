using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TerraFX.Interop.Windows;

namespace Primary.Memory.Native
{
    public unsafe readonly struct ArrayPtr<T> : IEquatable<ArrayPtr<T>> where T : unmanaged
    {
        private readonly T* _ptr;
        private readonly int _length;

        public ArrayPtr()
        {
            _ptr = null;
            _length = 0;
        }

        public ArrayPtr(T* ptr, int length)
        {
            _ptr = ptr;
            _length = length;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is ArrayPtr<T> self && Equals(self);
        public bool Equals(ArrayPtr<T> other)
        {
            return _ptr == other._ptr && _length == other._length;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((nint)_ptr, _length);
        }

        public override string ToString()
        {
            return $"{(nint)_ptr:x8}[{_length}]";
        }

        public Span<T> AsSpan()
        {
            return _ptr == null ? Span<T>.Empty : new Span<T>(_ptr, _length);
        }

        public Span<T> AsSpan(int start)
        {
            Guard.IsLessThanOrEqualTo((uint)start, (uint)_length);

            return _ptr == null ? Span<T>.Empty : new Span<T>(_ptr + start, _length - start);
        }

        public Span<T> AsSpan(int start, int count)
        {
            Guard.IsLessThanOrEqualTo((uint)count, (uint)(_length - start));
            Guard.IsLessThanOrEqualTo((uint)start, (uint)_length);
       
            return _ptr == null ? Span<T>.Empty : new Span<T>(_ptr + start, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArrayPtr<TTo> Cast<TTo>() where TTo : unmanaged
        {
            // refer to "MemoryMarshal.Cast<,>()" for details

            uint fromSize = (uint)sizeof(T);
            uint toSize = (uint)sizeof(TTo);
            uint fromLength = (uint)_length;
            int toLength;

            if (fromSize == toSize)
                toLength = (int)fromLength;
            else if (fromSize == 1)
                toLength = (int)(fromLength / toSize);
            else
            {
                ulong toLengthUInt64 = (ulong)fromLength * (ulong)fromSize / (ulong)toSize;
                toLength = checked((int)toLengthUInt64);
            }

            return new ArrayPtr<TTo>((TTo*)_ptr, toLength);
        }

        public ArrayPtr<T> Slice(int start)
        {
            Guard.IsLessThanOrEqualTo((uint)start, (uint)_length);
            return _ptr == null ? Null : new ArrayPtr<T>(_ptr + start, _length - start);
        }

        public ArrayPtr<T> Slice(int start, int count)
        {
            Guard.IsLessThanOrEqualTo((uint)count, (uint)(_length - start));
            Guard.IsLessThanOrEqualTo((uint)start, (uint)_length);
            return _ptr == null ? Null : new ArrayPtr<T>(_ptr + start, count);
        }

        public void CopyTo(Span<T> destination)
        {
            if ((uint)_length <= (uint)destination.Length)
                AsSpan().CopyTo(destination);
            else
                throw new ArgumentException("Not enough data in destination to copy");
        }

        public void CopyTo(ArrayPtr<T> destination)
        {
            if ((uint)_length <= (uint)destination.Length)
                NativeMemory.Copy(_ptr, destination._ptr, (nuint)_length);
            else
                throw new ArgumentException("Not enough data in destination to copy");
        }

        public bool TryCopyTo(Span<T> destination)
        {
            bool retVal = false;
            if ((uint)_length <= (uint)destination.Length)
            {
                AsSpan().CopyTo(destination);
                retVal = true;
            }

            return retVal;
        }

        public bool TryCopyTo(ArrayPtr<T> destination)
        {
            bool retVal = false;
            if ((uint)_length <= (uint)destination.Length)
            {
                NativeMemory.Copy(_ptr, destination._ptr, (nuint)_length);
                retVal = true;
            }

            return retVal;
        }

        public ref T this[int index]
        {
            get
            {
                Guard.IsLessThan((uint)index, (uint)_length);
                return ref _ptr[index];
            }
        }

        public T* Pointer => _ptr;
        public ref T Reference => ref Unsafe.AsRef<T>(_ptr);

        public int Length => _length;

        public bool IsNull => _ptr == null;
        public bool IsEmpty => _length == 0;

        public bool IsNullOrEmpty => _ptr == null || _length == 0;

        public static readonly ArrayPtr<T> Null = new ArrayPtr<T>();

        public static implicit operator Span<T>(ArrayPtr<T> array) => array.AsSpan();
    }
}
