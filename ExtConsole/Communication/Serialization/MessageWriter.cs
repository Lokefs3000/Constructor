using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ExtConsole.Communication.Serialization
{
    public sealed class MessageWriter
    {
        private byte[]? _pooledArray;
        private int _arrayLength;

        private int _position;

        internal MessageWriter()
        {
            _pooledArray = null;
            _arrayLength = 0;

            _position = 0;
        }

        internal void ResetForNewWrite()
        {
            _arrayLength = 0;
            _position = 0;
        }

        private void ResizeIfTooSmall(int minSize)
        {
            if (minSize == 0)
                return;

            if (_pooledArray == null || minSize >= _pooledArray?.Length)
            {
                minSize = (int)BitOperations.RoundUpToPowerOf2((uint)(minSize + 1));
                Array.Resize(ref _pooledArray, minSize);
            }

            _arrayLength = Math.Max(_arrayLength, minSize);
        }

        public int Seek(int offset, SeekOrigin origin)
        {
            _position = Math.Clamp(origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _arrayLength - offset,
                _ => throw new NotImplementedException()
            }, 0, _arrayLength);

            return _position;
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
                return;

            ResizeIfTooSmall(_position + buffer.Length);

            buffer.CopyTo(_pooledArray.AsSpan(_position));
            _position += buffer.Length;
        }

        public void Write(ReadOnlySpan<char> buffer)
        {
            if (buffer.IsEmpty)
                return;

            ResizeIfTooSmall(_position + buffer.Length * 2);

            MemoryMarshal.Cast<char, byte>(buffer).CopyTo(_pooledArray.AsSpan(_position));
            _position += buffer.Length * 2;
        }

        public void Write(bool value) => WriteTypeWithOffset(value);
        public void Write(char value) => WriteTypeWithOffset(value);

        public void Write(byte value) => WriteTypeWithOffset(value);
        public void Write(sbyte value) => WriteTypeWithOffset(value);

        public void Write(short value) => WriteTypeWithOffset(value);
        public void Write(int value) => WriteTypeWithOffset(value);
        public void Write(long value) => WriteTypeWithOffset(value);

        public void Write7BitEncodedInt(int value)
        {
            // Refer to "BinaryWriter.Write7BitEncodedInt"

            uint uValue = (uint)value;

            while (uValue > 0x7fu)
            {
                WriteTypeWithOffset((byte)(uValue | ~0x7fu));
                uValue >>= 7;
            }

            WriteTypeWithOffset((byte)uValue);
        }

        public void Write(ushort value) => WriteTypeWithOffset(value);
        public void Write(uint value) => WriteTypeWithOffset(value);
        public void Write(ulong value) => WriteTypeWithOffset(value);

        public void Write(Half value) => WriteTypeWithOffset(value);
        public void Write(float value) => WriteTypeWithOffset(value);
        public void Write(double value) => WriteTypeWithOffset(value);
        public void Write(decimal value) => WriteTypeWithOffset(value);

        public void Write(string value)
        {
            Write7BitEncodedInt(value.Length);

            ResizeIfTooSmall(_position + value.Length);
            for (int i = 0; i < value.Length; ++i)
            {
                _pooledArray![_position + i] = (byte)Math.Min(value[i], byte.MaxValue);
            }

            _position += value.Length;
        }

        private void WriteTypeWithOffset<T>(T value) where T : unmanaged
        {
            ResizeIfTooSmall(_position + Unsafe.SizeOf<T>());

            Unsafe.WriteUnaligned(ref _pooledArray![_position], value);
            _position += Unsafe.SizeOf<T>();
        }

        public int Position => _position;

        internal ReadOnlySpan<byte> Bytes => _pooledArray == null ? ReadOnlySpan<byte>.Empty : _pooledArray.AsSpan(0, _arrayLength);
    }
}
