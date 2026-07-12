using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Diagnostics;
using TerraFX.Interop.Windows;

namespace Primary.Streams
{
    public sealed class PooledMemoryStream : Stream
    {
        private ArrayPool<byte> _arrayPool;

        private byte[] _rentedArray;
        private readonly int _startCapacity;

        private int _position;
        private int _length;

        private int _actuallyWritten;

        private bool _disposedValue;

        public PooledMemoryStream(ArrayPool<byte>? arrayPool = null)
        {
            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;

            _rentedArray = [];
            _startCapacity = DefaultBufferSize;

            _position = 0;
            _length = 0;

            _actuallyWritten = 0;
        }

        public PooledMemoryStream(int capacity, ArrayPool<byte>? arrayPool = null)
        {
            Guard.IsGreaterThanOrEqualTo(capacity, 0);

            _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;

            _rentedArray = [];
            _startCapacity = capacity;

            _position = 0;
            _length = 0;

            _actuallyWritten = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_rentedArray.Length > 0)
                        _arrayPool.Return(_rentedArray);
                    _rentedArray = [];
                }

                _disposedValue = true;
            }
        }

        private void ExpandInternalArray()
        {
            byte[] newPool = _arrayPool.Rent(_length);

            if (_actuallyWritten > 0)
                Array.Copy(_rentedArray, newPool, _actuallyWritten);

            if (_rentedArray.Length > 0)
                _arrayPool.Return(_rentedArray);
            _rentedArray = newPool;
        }

        #region Stream
        public override void Flush()
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            if (offset == 0)
            {
                switch (origin)
                {
                    case SeekOrigin.Begin: _position = 0; break;
                    case SeekOrigin.End: _position = _length; break;
                }
            }
            else
            {
                if (origin == SeekOrigin.Current)
                    offset += _position;
                else if (origin == SeekOrigin.End)
                    offset += _length;

                _position = (int)Math.Clamp(offset, 0, int.MaxValue);
            }

            return _position;
        }

        public override void SetLength(long value)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            _length = (int)Math.Clamp(value, 0, MaxCapacity);
            _actuallyWritten = Math.Min(_actuallyWritten, _length);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (offset + count > buffer.Length)
                throw new ArgumentException("'Offset + Count' results in an overflow of the source buffer");

            // always expand if the length is larger than the internal array
            if (_length > _rentedArray.Length)
                ExpandInternalArray();

            int canBeRead = _length - _position - count;
            int actuallyRead = Math.Min(canBeRead, count);

            if (actuallyRead == 0)
                return 0;

            _rentedArray.AsSpan(_position, canBeRead).CopyTo(buffer.AsSpan(offset, actuallyRead));
            _position += actuallyRead;

            return actuallyRead;
        }

        public override int Read(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            // always expand if the length is larger than the internal array
            if (_length > _rentedArray.Length)
                ExpandInternalArray();

            int canBeRead = _length - _position - buffer.Length;
            int actuallyRead = Math.Min(canBeRead, buffer.Length);

            if (actuallyRead == 0)
                return 0;

            _rentedArray.AsSpan(_position, canBeRead).CopyTo(buffer[..actuallyRead]);
            _position += actuallyRead;

            return actuallyRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            if (offset + count > buffer.Length)
                throw new ArgumentException("'Offset + Count' results in an overflow of the source buffer");

            int positionAfter = (int)Math.Min(MaxCapacity, (long)(_position + count));
            int bytesToWrite = Math.Min(positionAfter - _position, count);

            if (bytesToWrite == 0)
                return;

            _length = Math.Max(_length, positionAfter);

            // always expand if the length is larger than the internal array
            if (_length > _rentedArray.Length)
                ExpandInternalArray();

            buffer.AsSpan(offset, bytesToWrite).CopyTo(_rentedArray.AsSpan(_position, bytesToWrite));

            _actuallyWritten = Math.Max(_actuallyWritten, positionAfter);
            _position = positionAfter;

            Debug.Assert(_rentedArray.Length >= _actuallyWritten);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            int positionAfter = (int)Math.Min(MaxCapacity, (long)(_position + buffer.Length));
            int bytesToWrite = Math.Min(positionAfter - _position, buffer.Length);

            if (bytesToWrite == 0)
                return;

            _length = Math.Max(_length, positionAfter);

            // always expand if the length is larger than the internal array
            if (_length > _rentedArray.Length)
                ExpandInternalArray();

            buffer[..bytesToWrite].CopyTo(_rentedArray.AsSpan(_position, bytesToWrite));

            _actuallyWritten = Math.Max(_actuallyWritten, positionAfter);
            _position = positionAfter;

            Debug.Assert(_rentedArray.Length >= _actuallyWritten);
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);

            if (_length > 0)
            {
                // always expand if the length is larger than the internal array
                if (_length > _rentedArray.Length)
                    ExpandInternalArray();

                destination.Write(AsSpan());
            }
        }
        #endregion

        public ReadOnlySpan<byte> AsSpan() => _rentedArray.AsSpan(0, _length);

        public bool IsEmpty => _length == 0;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;

        public override long Length => _length;
        public override long Position { get => _position; set => Seek(value, SeekOrigin.Begin); }

        public const int DefaultBufferSize = 256;
        public const int MaxCapacity = int.MaxValue;
    }
}
