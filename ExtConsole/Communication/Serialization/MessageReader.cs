using CommunityToolkit.HighPerformance;
using Primary.Pooling;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ExtConsole.Communication.Serialization
{
    public sealed class MessageReader
    {
        private byte[]? _pooledArray;
        private int _arrayLength;

        private int _position;

        internal MessageReader()
        {
            _pooledArray = null;
            _arrayLength = 0;

            _position = 0;
        }

        internal void ReturnArray()
        {
            if (_pooledArray != null)
                ArrayPool<byte>.Shared.Return(_pooledArray);

            _pooledArray = null;
            _arrayLength = 0;

            _position = 0;
        }

        internal void RentArray(ReadOnlySpan<byte> data)
        {
            if (_pooledArray != null)
                ArrayPool<byte>.Shared.Return(_pooledArray);

            _pooledArray = ArrayPool<byte>.Shared.Rent(data.Length);
            _arrayLength = data.Length;

            _position = 0;

            data.CopyTo(_pooledArray);
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

        public void Read()
        {
            if (_position == _arrayLength)
                return;
            ++_position;
        }

        public int Read(Span<byte> buffer)
        {
            if (_position > _arrayLength || buffer.IsEmpty)
                return 0;

            int available = Math.Min(_arrayLength - _position, buffer.Length);

            _pooledArray.AsSpan(_position, available).CopyTo(buffer);
            _position += available;

            return available;
        }

        public int Read(Span<char> buffer)
        {
            if (_position > _arrayLength || buffer.IsEmpty)
                return 0;

            int available = Math.Min((_arrayLength - _position) / 2, buffer.Length) * 2;

            _pooledArray.AsSpan(_position, available).CopyTo(MemoryMarshal.Cast<char, byte>(buffer));
            _position += available;

            return available;
        }

        public void ReadExactly(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
                return;
            if (_position + buffer.Length > _arrayLength)
                throw new EndOfStreamException();

            _pooledArray.AsSpan(_position, buffer.Length).CopyTo(buffer);
            _position += buffer.Length;
        }

        public bool ReadBoolean() => ReadTypeWithOffset<bool>();
        public char ReadChar() => ReadTypeWithOffset<char>();

        public byte ReadByte() => ReadTypeWithOffset<byte>();
        public sbyte ReadSByte() => ReadTypeWithOffset<sbyte>();

        public short ReadInt16() => ReadTypeWithOffset<short>();
        public int ReadInt32() => ReadTypeWithOffset<int>();
        public long ReadInt64() => ReadTypeWithOffset<long>();

        public int Read7BitEncodedInt()
        {
            // Refer to "BinaryReader.Read7BitEncodedInt"

            uint result = 0;
            byte byteReadJustNow;

            const int MaxBytesWithoutOverflow = 4;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                byteReadJustNow = ReadByte();
                result |= (byteReadJustNow & 0x7Fu) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (int)result; // early exit
                }
            }

            byteReadJustNow = ReadByte();
            if (byteReadJustNow > 0b_1111u)
            {
                throw new FormatException("Bad 7-bit int");
            }

            result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (int)result;
        }

        public ushort ReadUInt16() => ReadTypeWithOffset<ushort>();
        public uint ReadUInt32() => ReadTypeWithOffset<uint>();
        public ulong ReadUInt64() => ReadTypeWithOffset<ulong>();

        public Half ReadHalf() => ReadTypeWithOffset<Half>();
        public float ReadSingle() => ReadTypeWithOffset<float>();
        public double ReadDouble() => ReadTypeWithOffset<double>();
        public decimal ReadDecimal() => ReadTypeWithOffset<decimal>();

        public string ReadString()
        {
            int length = Read7BitEncodedInt();
            if (_position + length > _arrayLength)
                throw new EndOfStreamException();

            Span<byte> span = _pooledArray.AsSpan(_position, length);
            _position += length;

            unsafe
            {
                fixed (byte* ptr = span)
                {
                    return new string((sbyte*)ptr, 0, length);
                }
            }
        }

        private T ReadTypeWithOffset<T>() where T : unmanaged
        {
            if (_position + Unsafe.SizeOf<T>() > _arrayLength)
                throw new EndOfStreamException();

            ref byte v = ref _pooledArray![_position];
            _position += Unsafe.SizeOf<T>();
            return Unsafe.ReadUnaligned<T>(ref v);
        }

        public int Position => _position;

        internal readonly record struct Policy : IObjectPoolPolicy<MessageReader>
        {
            public MessageReader Create() => new MessageReader();
            public bool Return(ref MessageReader obj)
            {
                obj.ReturnArray();
                return true;
            }
        }
    }
}
