using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Serialization
{
    public sealed class SDFReader
    {
        private char[] _inputBuffer;

        private int _revertIndex;
        private int _currentIndex;

        private Stack<CurrentContext> _recContext;
        private Stack<CurrentContext> _context;

        internal SDFReader(char[] inputStream)
        {
            _inputBuffer = inputStream;

            _revertIndex = 0;
            _currentIndex = 0;

            _recContext = new Stack<CurrentContext>();
            _context = new Stack<CurrentContext>();
        }

        #region Manipulation
        private void SkipWhitespace()
        {
            while (IsEOF || char.IsWhiteSpace(Peek()))
            {
                Advance();
            }
        }

        private ReadOnlySpan<char> ReadIdentifier()
        {
            SkipWhitespace();

            if (!char.IsLetter(Peek()))
                return ReadOnlySpan<char>.Empty;

            int start = _currentIndex;
            do
            {
                Advance();
                ThrowIfEOF();
            } while (char.IsLetterOrDigit(Peek()));

            return _inputBuffer.AsSpan(start, _currentIndex - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char Peek() => IsEOF ? '\0' : _inputBuffer[_currentIndex];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char Advance() => IsEOF ? '\0' : _inputBuffer[_currentIndex++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AdvanceIfMatch(char match)
        {
            if (Peek() == match)
            {
                Advance();
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfEOF()
        {
            if (IsEOF)
                throw new ArgumentOutOfRangeException("EOF");
        }

        public bool IsEOF => _currentIndex >= _inputBuffer.Length;
        #endregion

        public void Commit()
        {
            _revertIndex = _currentIndex;

            _recContext.Clear();
            foreach (CurrentContext context in _context)
                _recContext.Push(context);
        }

        public void Restore()
        {
            _currentIndex = _revertIndex;

            _context.Clear();
            foreach (CurrentContext context in _recContext)
                _context.Push(context);
        }

        public bool TryPeekObject(out string? name)
        {
            name = null;

            int start = _currentIndex;

            ReadOnlySpan<char> identifier = ReadIdentifier();
            if (identifier.IsEmpty)
            {
                _currentIndex = start;
                return false;
            }

            SkipWhitespace();
            if (!AdvanceIfMatch('{'))
            {
                _currentIndex = start;
                return false;
            }

            name = identifier.ToString();

            _currentIndex = start;
            return true;
        }

        public bool BeginObject(out string? name)
        {
            name = null;

            ReadOnlySpan<char> identifier = ReadIdentifier();
            if (identifier.IsEmpty)
                return false;

            SkipWhitespace();
            if (!AdvanceIfMatch('{'))
                return false;

            name = identifier.ToString();

            _context.Push(CurrentContext.Object);
            return true;
        }

        public bool EndObject()
        {
            if (!_context.TryPeek(out CurrentContext context) || context != CurrentContext.Object)
                return false;

            _context.Pop();
            if (_context.TryPeek(out context) && context == CurrentContext.Property)
                _context.Pop();

            SkipWhitespace();
            return AdvanceIfMatch('}');
        }

        public bool BeginArray()
        {
            if (!_context.TryPeek(out CurrentContext context) || (context != CurrentContext.Property && context != CurrentContext.Array))
                return false;

            SkipWhitespace();
            if (!AdvanceIfMatch('['))
                return false;

            _context.Push(CurrentContext.Array);
            return true;
        }

        public bool EndArray()
        {
            if (!_context.TryPeek(out CurrentContext context) || context != CurrentContext.Array)
                return false;

            _context.Pop();
            if (_context.TryPeek(out context) && context == CurrentContext.Property)
                _context.Pop();

            SkipWhitespace();
            if (!AdvanceIfMatch(']'))
                return false;

            return true;
        }

        public bool ReadProperty(out string? name)
        {
            name = null;

            if (!_context.TryPeek(out CurrentContext context) || context != CurrentContext.Object)
                return false;

            ReadOnlySpan<char> identifier = ReadIdentifier();
            if (identifier.IsEmpty)
                return false;

            SkipWhitespace();
            if (!AdvanceIfMatch('='))
                return false;

            name = identifier.ToString();

            _context.Push(CurrentContext.Property);
            return true;
        }

        private bool ReadGeneric(out ReadOnlySpan<char> value, Func<char, bool> isValidCharacter)
        {
            value = ReadOnlySpan<char>.Empty;

            if (!_context.TryPeek(out CurrentContext context) || (context != CurrentContext.Property && context != CurrentContext.Array))
                return false;

            SkipWhitespace();

            int start = 0;
            do
            {
                Advance();
                ThrowIfEOF();
            } while (isValidCharacter(Peek()));

            value = _inputBuffer.AsSpan(start, _currentIndex - start);

            if (_context.TryPeek(out context))
            {
                if (context == CurrentContext.Property)
                    _context.Pop();
                else if (context == CurrentContext.Array)
                {
                    SkipWhitespace();
                    if (Peek() == ',')
                        Advance();
                }
            }

            return !value.IsEmpty;
        }

        private bool ReadTemplated<T>(out T value, Func<char, bool> validCharFunc, TemplatedConvert<T> converter) where T : struct
        {
            value = default;
            if (ReadGeneric(out ReadOnlySpan<char> span, validCharFunc))
            {
                return converter(span, out value);
            }

            return false;
        }

        private delegate bool TemplatedConvert<T>(ReadOnlySpan<char> span, out T value);

        public bool Read(out string? value)
        {
            value = null;
            if (ReadGeneric(out ReadOnlySpan<char> span, static (c) => char.IsLetterOrDigit(c) || c == '"' || c == '\''))
            {
                if (!span.StartsWith('"') || !span.EndsWith('"'))
                    return false;

                value = span.Slice(1, span.Length - 2).ToString();
                return true;
            }

            return false;
        }

        public bool Read<T>(out T? value)
        {
            value = default;
            if (ReadGeneric(out ReadOnlySpan<char> span, (x) => char.IsLetterOrDigit(x)))
            {
                if (SerializerTypes.TryDeserializeDataType<T>(this, out value))
                    return true;
            }

            return false;
        }

        public bool Read(out float value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c) || char.IsPunctuation(c), static (ReadOnlySpan<char> span, out float v) => float.TryParse(span, out v));
        public bool Read(out double value)
         => ReadTemplated(out value, static (c) => char.IsDigit(c) || char.IsPunctuation(c), static (ReadOnlySpan<char> span, out double v) => double.TryParse(span, out v));
        public bool Read(out sbyte value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c), static (ReadOnlySpan<char> span, out sbyte v) => sbyte.TryParse(span, out v));
        public bool Read(out short value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c), static (ReadOnlySpan<char> span, out short v) => short.TryParse(span, out v));
        public bool Read(out int value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c), static (ReadOnlySpan<char> span, out int v) => int.TryParse(span, out v));
        public bool Read(out long value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c), static (ReadOnlySpan<char> span, out long v) => long.TryParse(span, out v));
        public bool Read(out byte value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c), static (ReadOnlySpan<char> span, out byte v) => byte.TryParse(span, out v));
        public bool Read(out ushort value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c), static (ReadOnlySpan<char> span, out ushort v) => ushort.TryParse(span, out v));
        public bool Read(out uint value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c), static (ReadOnlySpan<char> span, out uint v) => uint.TryParse(span, out v));
        public bool Read(out ulong value)
            => ReadTemplated(out value, static (c) => char.IsDigit(c), static (ReadOnlySpan<char> span, out ulong v) => ulong.TryParse(span, out v));

        public T? Read<T>(string propertyName)
        {
            if (!ReadProperty(out string? property) || property != propertyName)
                return default;

            Read(out T? value);
            return value;
        }

        public bool IsObjectActive
        {
            get
            {
                int start = _currentIndex;

                SkipWhitespace();
                if (Peek() == '}')
                {
                    _currentIndex = start;
                    return true;
                }

                _currentIndex = start;
                return false;
            }
        }

        private enum CurrentContext : byte
        {
            Object,
            Property,
            Array
        }
    }
}
