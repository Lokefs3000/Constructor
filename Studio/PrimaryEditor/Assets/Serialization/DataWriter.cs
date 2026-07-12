using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;

namespace PrimaryEditor.Assets.Serialization
{
    public ref struct DataWriter : IDisposable
    {
        private readonly Stream _stream;
        private bool _isLineFresh;

        public DataWriter(Stream stream)
        {
            _stream = stream;
            _isLineFresh = true;
        }

        public readonly void Dispose()
        {
            _stream.Dispose();
        }

        public void WriteVersionHeader(int version)
        {
            Span<byte> serialized = stackalloc byte[10];
            ((uint)version).TryFormat(serialized, out int _);

            _stream.Write("@version|"u8);
            _stream.Write(serialized);
            _stream.Write("\r\n"u8);

            _isLineFresh = true;
        }

        public void WriteNull()
        {
            if (!_isLineFresh)
                _stream.Write((byte)'|');
            else
                _isLineFresh = false;

            _stream.Write("null"u8);
        }

        public void WriteValue(string value)
        {
            if (!_isLineFresh)
                _stream.Write((byte)'|');
            else
                _isLineFresh = false;

            for (int i = 0; i < value.Length; ++i)
            {
                _stream.Write((byte)value[i]);
            }
        }

        public void WriteValue(Guid guid)
        {
            if (!_isLineFresh)
                _stream.Write((byte)'|');
            else
                _isLineFresh = false;

            Span<byte> serialized = stackalloc byte[32];
            guid.TryFormat(serialized, out _, "N");

            _stream.Write(serialized);
        }

        public void WriteValue(DateTime time)
        {
            if (!_isLineFresh)
                _stream.Write((byte)'|');
            else
                _isLineFresh = false;

            Span<byte> serialized = stackalloc byte[16];
            ((ulong)time.Ticks).TryFormat(serialized, out _, "x16");

            _stream.Write(serialized);
        }

        public void WriteValue(bool value)
        {
            if (!_isLineFresh)
                _stream.Write((byte)'|');
            else
                _isLineFresh = false;

            if (value)
                _stream.Write("!true"u8);
            else
                _stream.Write("!false"u8);
        }

        public void FinishLine()
        {
            _stream.Write("\r\n"u8);
            _isLineFresh = true;
        }
    }
}
