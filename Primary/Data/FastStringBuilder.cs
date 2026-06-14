using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Data
{
    public class FastStringBuilder
    {
        private char[] _array;

        private int _index;
        private int _length;

        private StringBuilderMode _mode;

        public FastStringBuilder()
        {
            _array = Array.Empty<char>();

            _index = 0;
            _length = 0;

            _mode = StringBuilderMode.Replace;
        }

        private void ResizeArray(int length, bool dontPadLength = false)
        {
            if (length == 0)
                _array = Array.Empty<char>();
            else
            {
                if (!dontPadLength && !int.IsPow2(length))
                {
                    length = (int)BitOperations.RoundUpToPowerOf2((uint)length);
                }

                if (length != _array.Length)
                {
                    Array.Resize(ref _array, length);
                }
            }
        }

        private void PushArrayData(int index, int newIndex)
        {
            if (index == newIndex)
                return;

            int length = _length - index;
            if (length == 0)
                return;

            Debug.Assert(length > 0);
            Debug.Assert(newIndex + length <= _length);

            Array.Copy(_array, index, _array, index, length);

            _length += Math.Abs(newIndex - index);
        }

        private void SetLength(int newLength)
        {
            Guard.IsGreaterThanOrEqualTo(newLength, 0);

            if (newLength > _array.Length)
                ResizeArray(newLength);

            _length = newLength;
            _index = Math.Min(_index, newLength);
        }

        private void SetCapacity(int newCapacity)
        {
            Guard.IsGreaterThanOrEqualTo(newCapacity, 0);

            if (_array.Length == newCapacity)
                return;
            ResizeArray(newCapacity, true);

            _length = Math.Min(_length, newCapacity);
            _index = Math.Min(_index, newCapacity);
        }

        #region State
        public void Seek(int offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: _index = Math.Clamp(offset, 0, _length); break;
                case SeekOrigin.Current: _index = Math.Clamp(_index + offset, 0, _length); break;
                case SeekOrigin.End: _index = Math.Clamp(_length + offset, 0, _length); break;
            }
        }

        public void EnsureCapacity(int length)
        {
            if (_length + length > _array.Length)
                ResizeArray(_length + length);
        }
        #endregion
        #region Manipulation
        public void Clear()
        {
            _length = 0;
            _index = 0;
        }

        public void Append(char c)
        {
            if (_mode == StringBuilderMode.Replace)
            {
                if (_index + 1 > _array.Length)
                    ResizeArray(_index + 1);

                _array[_index++] = c;
            }
            else
            {
                if (Math.Max(_index, _length) + 1 > _array.Length)
                    ResizeArray(Math.Max(_index, _length) + 1);

                if (_index > _length)
                {
                    PushArrayData(_index, _index + 1);

                    _array[_index++] = c;
                }
                else
                {
                    _array[_index++] = c;
                    _length = _index;
                }
            }
        }

        public void Append(ReadOnlySpan<char> span)
        {
            if (_mode == StringBuilderMode.Replace)
            {
                if (_index + span.Length > _array.Length)
                    ResizeArray(_index + span.Length);

                span.CopyTo(_array.AsSpan(_index));

                _index += span.Length;
                _length = Math.Max(_index, _length);
            }
            else
            {
                if (Math.Max(_index, _length) + span.Length > _array.Length)
                    ResizeArray(Math.Max(_index, _length) + span.Length);

                if (_index < _length)
                {
                    PushArrayData(_index, _index + span.Length);

                    span.CopyTo(_array.AsSpan(_index));

                    _index += span.Length;
                }
                else
                {
                    span.CopyTo(_array.AsSpan(_index));

                    _index += span.Length;
                    _length = _index;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append<T>(T value, ReadOnlySpan<char> format, IFormatProvider? provider) where T : ISpanFormattable
        {
            if (_mode == StringBuilderMode.Replace)
                Append_Replace(value, format, provider);
            else
                Append_Insert(value, format, provider);
        }

        private void Append_Replace<T>(T value, ReadOnlySpan<char> format, IFormatProvider? provider) where T : ISpanFormattable
        {
            if (_index + SpanFormattableBufferSize > _array.Length)
                ResizeArray(_index + SpanFormattableBufferSize);

            if (!value.TryFormat(_array.AsSpan(_index), out int charsWritten, format, provider))
                throw new Exception($"Failed to format value: {value}");

            _index += charsWritten;
            _length = Math.Max(_index, _length);
        }

        private void Append_Insert<T>(T value, ReadOnlySpan<char> format, IFormatProvider? provider) where T : ISpanFormattable
        {
            ReadOnlySpan<char> temp = stackalloc char[SpanFormattableBufferSize];
            if (!value.TryFormat(_array.AsSpan(_index), out int charsWritten, format, provider))
                throw new Exception($"Failed to format value: {value}");

            if (Math.Max(_index, _length) + charsWritten > _array.Length)
                ResizeArray(Math.Max(_index, _length) + charsWritten);

            if (_index < _length)
            {
                PushArrayData(_index, _index + charsWritten);

                temp[..charsWritten].CopyTo(_array.AsSpan(_index));

                _index += charsWritten;
            }
            else
            {
                temp[..charsWritten].CopyTo(_array.AsSpan(_index));

                _index += charsWritten;
                _length = _index;
            }
        }
        #endregion

        /// <summary>Internally invokes the <see cref="SpanExtensions.GetDjb2HashCode{T}(Span{T})"/> method.</summary>
        public override int GetHashCode() => _array.AsSpan(0, _length).GetDjb2HashCode();
        public override string ToString() => _length == 0 ? string.Empty : new string(_array, 0, _length);

        public Span<char> AsSpan() => _array.AsSpan(0, _length);
        public Span<char> AsSpan(int start) => _array.AsSpan(start);
        public Span<char> AsSpan(int start, int length) => _array.AsSpan(start, length);

        public ref char GetReference()
        {
            Guard.IsGreaterThan(_array.Length, 0);
            return ref _array.DangerousGetReference();
        }

        public ref char GetReferenceAt(int i)
        {
            Debug.Assert(_length <= _array.Length);
            Guard.IsInRange(i, 0, _length);
            return ref _array.DangerousGetReferenceAt(i);
        }

        public int Index { get => _index; set => Seek(value, SeekOrigin.Begin); }
        public int Length { get => _length; set => SetLength(value); }
        public int Capacity { get => _array.Length; set => SetCapacity(value); }

        public StringBuilderMode Mode { get => _mode; set => _mode = value; }

        private const int SpanFormattableBufferSize = 40;
    }

    public enum StringBuilderMode : byte
    {
        /// <summary>Replace text when writing over already written sections.</summary>
        Replace = 0,
        /// <summary>Push back already written sections when writing.</summary>
        Insert
    }

    public static class FastStringBuilderExt
    {
        public static void Append(this FastStringBuilder self, string value) => self.Append(value.AsSpan());

        public static void Append<T>(this FastStringBuilder self, T value, ReadOnlySpan<char> format) where T : ISpanFormattable => self.Append(value, format, null);
        public static void Append<T>(this FastStringBuilder self, T value, IFormatProvider? provider) where T : ISpanFormattable => self.Append(value, ReadOnlySpan<char>.Empty, provider);
        public static void Append<T>(this FastStringBuilder self, T value, string format, IFormatProvider? provider) where T : ISpanFormattable => self.Append(value, format.AsSpan(), provider);
        public static void Append<T>(this FastStringBuilder self, T value, string format) where T : ISpanFormattable => self.Append(value, format.AsSpan(), null);
        public static void Append<T>(this FastStringBuilder self, T value) where T : ISpanFormattable => self.Append(value, ReadOnlySpan<char>.Empty, null);
    }
}
