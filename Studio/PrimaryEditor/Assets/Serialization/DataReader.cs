using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Primary.Collections;

namespace PrimaryEditor.Assets.Serialization
{
    public ref struct DataReader : IDisposable
    {
        private readonly Stream _stream;
        private bool _isLineFresh;

        private byte _leadingByte;
        private RentedList<byte> _currentValueBuffer;

        private int _lineIndex;
        private int _lineStart;

        public DataReader(Stream stream)
        {
            _stream = stream;
            _isLineFresh = true;

            _leadingByte = (byte)stream.ReadByte();
            _currentValueBuffer = new RentedList<byte>();

            _lineIndex = 0;
            _lineStart = 0;
        }

        public void Dispose()
        {
            _stream.Dispose();
            _currentValueBuffer.Dispose();
        }

        public int ReadVersionHeader()
        {
            if (!_isLineFresh)
                return -1;

            ReadOnlySpan<byte> value = ReadValue();
            if (!value.SequenceEqual("@version"u8))
                throw CreateFailureException($"Expected version header decleration '{Encoding.UTF8.GetString(value)}'");

            if (_leadingByte != '|')
                throw CreateFailureException("Expected value after version header");
            _leadingByte = (byte)_stream.ReadByte();

            value = ReadValue();

            if (_leadingByte != '\n' && _leadingByte != byte.MaxValue)
                throw CreateFailureException("Expected new line after version header value");

            int result = -1;
            if (value.IsEmpty || !int.TryParse(value, out result))
                throw CreateFailureException($"Invalid version header value '{Encoding.UTF8.GetString(value)}'");

            ++_lineIndex;
            _lineStart = (int)_stream.Position;

            _leadingByte = (byte)_stream.ReadByte();
            return result;
        }

        public string? ReadString()
        {
            if (_isLineFresh)
                _isLineFresh = false;
            else if (_leadingByte != '|')
                throw CreateFailureException("Expected deliminator after previous value in list");
            else
                _leadingByte = (byte)_stream.ReadByte();

            ReadOnlySpan<byte> value = ReadValue();
            return value.SequenceEqual("null"u8) ? null : Encoding.UTF8.GetString(value);
        }

        public Guid? ReadGuid()
        {
            if (_isLineFresh)
                _isLineFresh = false;
            else if (_leadingByte != '|')
                throw CreateFailureException("Expected deliminator after previous value in list");
            else
                _leadingByte = (byte)_stream.ReadByte();

            ReadOnlySpan<byte> value = ReadValue();
            if (value.SequenceEqual("null"u8))
                return null;

            if (Guid.TryParse(value, out Guid result))
                return result;
            else
                throw CreateFailureException($"Failed to parse guid from string '{Encoding.UTF8.GetString(value)}'");
        }

        public DateTime? ReadDateTime()
        {
            if (_isLineFresh)
                _isLineFresh = false;
            else if (_leadingByte != '|')
                throw CreateFailureException("Expected deliminator after previous value in list");
            else
                _leadingByte = (byte)_stream.ReadByte();

            ReadOnlySpan<byte> value = ReadValue();
            if (value.SequenceEqual("null"u8))
                return null;

            if (ulong.TryParse(value, NumberStyles.HexNumber, null, out ulong result))
                return new DateTime((long)result);
            else
                throw CreateFailureException($"Failed to parse date time from string '{Encoding.UTF8.GetString(value)}'");
        }

        public bool? ReadBoolean()
        {
            if (_isLineFresh)
                _isLineFresh = false;
            else if (_leadingByte != '|')
                throw CreateFailureException("Expected deliminator after previous value in list");
            else
                _leadingByte = (byte)_stream.ReadByte();

            ReadOnlySpan<byte> value = ReadValue();
            if (value.SequenceEqual("null"u8))
                return null;

            if (value.SequenceEqual("!true"u8))
                return true;
            else if (value.SequenceEqual("!false"u8))
                return false;
            else
                throw CreateFailureException($"Failed to parse boolean from string '{Encoding.UTF8.GetString(value)}'");
        }

        public int? ReadInt32()
        {
            if (_isLineFresh)
                _isLineFresh = false;
            else if (_leadingByte != '|')
                throw CreateFailureException("Expected deliminator after previous value in list");
            else
                _leadingByte = (byte)_stream.ReadByte();

            ReadOnlySpan<byte> value = ReadValue();
            if (value.SequenceEqual("null"u8))
                return null;

            if (int.TryParse(value, null, out int result))
                return result;
            else
                throw CreateFailureException($"Failed to parse int32 from string '{Encoding.UTF8.GetString(value)}'");
        }

        public void ReadNewLine()
        {
            if (_stream.Position >= _stream.Length)
                return;

            if (_leadingByte != '\n')
                throw CreateFailureException($"Expected new line control");

            ++_lineIndex;
            _lineStart = (int)_stream.Position;

            _leadingByte = (byte)_stream.ReadByte();
            _isLineFresh = true;
        }

        private ReadOnlySpan<byte> ReadValue()
        {
            if (_leadingByte == '|' || _leadingByte == '\n' || _leadingByte == byte.MaxValue)
                return [];

            _currentValueBuffer.Clear();
            _currentValueBuffer.Add(_leadingByte);

            while (true)
            {
                _leadingByte = (byte)_stream.ReadByte();
                if (_leadingByte == '|' || _leadingByte == '\n' || _leadingByte == byte.MaxValue)
                    return _currentValueBuffer.AsSpan();

                if (_leadingByte != '\r')
                    _currentValueBuffer.Add(_leadingByte);
            }
        }

        private Exception CreateFailureException(string message)
        {
            return new Exception($"{_lineIndex}:{(int)_stream.Position - _lineStart}: {message}");
        }

        public readonly byte LeadingByte => _leadingByte;
        public readonly bool IsAtEndOfStream => _stream.Position >= _stream.Length;
    }
}
