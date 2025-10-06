using CommunityToolkit.HighPerformance;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Primary.Serialization
{
    public ref struct SDFReader
    {
        private ReadOnlySpan<char> _source;
        private SDFTokenType _lastReadToken;

        private ReadOnlySpan<char> _slice;

        private int _line;
        private int _lineIndex;

        private int _index;

        private Stack<TokenContext> _context;

        public SDFReader(ReadOnlySpan<char> source)
        {
            _source = source;
            _lastReadToken = SDFTokenType.Unknown;

            _slice = ReadOnlySpan<char>.Empty;

            _line = 0;
            _lineIndex = 0;

            _index = 0;

            _context = new Stack<TokenContext>();
        }

        #region Basic
        private char Peek() => IsEOF ? '\0' : _source.DangerousGetReferenceAt(_index);
        private char PeekPrev()
        {
            uint prev = (uint)_index - 1;
            if (prev < _index)
                return _source.DangerousGetReferenceAt((int)prev);
            return '\0';
        }
        private char Advance()
        {
            if (IsEOF)
                return '\0';
            char c = _source.DangerousGetReferenceAt(_index++);
            if (c == '\n')
            {
                _line++;
                _lineIndex = _index;
            }
            return c;
        }

        private bool CheckContext(TokenContext expectedContext)
        {
            if (_context.TryPeek(out TokenContext currentContext) && currentContext == expectedContext)
                return true;
            return false;
        }

        private bool IsValueTypeValid()
        {
            if (!_context.TryPeek(out TokenContext context))
                return false;

            if (context != TokenContext.WithinArray)
                return _lastReadToken == SDFTokenType.Property;

            return true;
        }

        private bool IsPropertyNameValid()
        {
            if (CheckContext(TokenContext.WithinArray))
                return false;

            if (IsCharSkippable(Peek()))
            {
                Advance();

                while (!IsEOF && IsCharSkippable(Peek())) Advance();
            }

            if (Peek() != '=')
                return false;

            Advance();
            return true;
        }

        private bool IsSignedObjectValid()
        {
            if (CheckContext(TokenContext.WithinArray))
                return false;

            if (_lastReadToken == SDFTokenType.Property)
                return false;

            return true;
        }

        private void CheckForArrayDeliminter()
        {
            if (Peek() == ']')
                return;

            if (IsCharSkippable(Peek()))
            {
                Advance();

                while (!IsEOF && IsCharSkippable(Peek())) Advance();
            }

            if (Peek() != ']')
            {
                if (Peek() != ',')
                    ThrowParseException("Values inside of array must be split with a ',' delimiter");

                Advance();
            }
        }

        [DoesNotReturn, StackTraceHidden]
        private void ThrowParseException(string message) => throw new SDFParseException($"[{_line}:{_index - _lineIndex}]: {message}");

        public bool IsEOF => _index >= _source.Length;

        private static bool IsDeliminationCapable(char c) => char.IsWhiteSpace(c) || char.IsControl(c);
        private static bool IsIdentifierCapable(char c) => char.IsLetterOrDigit(c) || c == '.' || c == '_';
        #endregion

        #region Parsing
        private bool TryParseNextToken()
        {
            char token = Advance();
            switch (token)
            {
                //in-line

                case '{': //value type object begin
                    {
                        if (!IsValueTypeValid())
                            ThrowParseException($"A value type object is not valid in this context");

                        _context.Push(TokenContext.WithinObject);
                        _lastReadToken = SDFTokenType.ObjectBegin;
                        _slice = ReadOnlySpan<char>.Empty;

                        return true;
                    }
                case '}': //(value type) object end
                    {
                        if (!CheckContext(TokenContext.WithinObject))
                            ThrowParseException($"Unexpected object end token outside of object context");

                        _context.Pop();
                        _lastReadToken = SDFTokenType.ObjectEnd;
                        _slice = ReadOnlySpan<char>.Empty;

                        return true;
                    }
                case '[': //array begin
                    {
                        if (!IsValueTypeValid())
                            ThrowParseException($"An array is not valid in this context");

                        _context.Push(TokenContext.WithinArray);
                        _lastReadToken = SDFTokenType.ArrayBegin;

                        return true;
                    }
                case ']':
                    {
                        if (!CheckContext(TokenContext.WithinArray))
                            ThrowParseException($"Unexpected array end token outside of array context");

                        _context.Pop();
                        _lastReadToken = SDFTokenType.ArrayEnd;

                        return true;
                    }
                case '\n':
                    {
                        _line++;
                        _lineIndex = _index;
                        return true;
                    }
                case '\0': return false;

                //proxy

                case '-':
                    {
                        if (char.IsDigit(Peek()))
                        {
                            if (!IsValueTypeValid())
                                ThrowParseException($"A number value is not valid in this context");
                            return ParseNumberValue(true);
                        }
                        break;
                    }
                case '+':
                    {
                        if (char.IsDigit(Peek()))
                        {
                            if (!IsValueTypeValid())
                                ThrowParseException($"A number value is not valid in this context");
                            return ParseNumberValue(true);
                        }
                        break;
                    }

                case '"':
                case '\'':
                    {
                        if (!IsValueTypeValid())
                            ThrowParseException($"A string value is not valid in this context");
                        return ParseStringValue(token);
                    }

                default:
                    {
                        if (char.IsDigit(token))
                        {
                            if (!IsValueTypeValid())
                                ThrowParseException($"A number value is not valid in this context");
                            return ParseNumberValue(false);
                        }
                        else if (char.IsLetter(token) || token == '_' || token == '@')
                        {
                            ReadOnlySpan<char> identifier = ReadIdentifier(true);
                            if (Peek() == '{')
                            {
                                Advance();
                                if (!IsSignedObjectValid())
                                    ThrowParseException("A signed object is not valid in this context");

                                _context.Push(TokenContext.WithinObject);
                                _lastReadToken = SDFTokenType.ObjectBegin;
                                _slice = identifier;
                                return true;
                            }
                            else if (identifier.Equals("true", StringComparison.OrdinalIgnoreCase) || identifier.Equals("false", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!IsValueTypeValid())
                                    ThrowParseException($"A boolean value is not valid in this context");

                                _lastReadToken = SDFTokenType.Boolean;
                                _slice = identifier;
                                return true;
                            }
                            else if (!identifier.Contains('@'))
                            {
                                if (!IsPropertyNameValid())
                                    ThrowParseException("A property name is not valid in this context");

                                _lastReadToken = SDFTokenType.Property;
                                _slice = identifier;
                                return true;
                            }
                        }

                        break;
                    }
            }

            ThrowParseException($"Unexpected token encountered: {token}");
            return false;
        }

        private bool ParseNumberValue(bool hasSignInPreviousToken)
        {
            bool isValueNegative = false;
            if (hasSignInPreviousToken)
            {
                char prev = _source[_index - 1];
                isValueNegative = prev == '-';
            }

            bool hasDecimalInNumber = false;

            int previous = hasSignInPreviousToken ? _index : _index - 1;
            while (true)
            {
                char c = Peek();
                if (c == '.')
                {
                    if (hasDecimalInNumber)
                        ThrowParseException("Number must not have more then 1 decimal in place");
                    hasDecimalInNumber = true;
                }
                else if (!char.IsDigit(c))
                    break;

                Advance();
            }

            int sliceIndex = _index;

            if (CheckContext(TokenContext.WithinArray))
                CheckForArrayDeliminter();
            else if (!IsDeliminationCapable(Peek()))
                ThrowParseException("Expected empty character or EOF after number");

            _lastReadToken = SDFTokenType.Number;
            _slice = _source.Slice(previous, sliceIndex - previous);

            return true;
        }

        private bool ParseStringValue(char delimiter)
        {
            int previous = _index;
            while (true)
            {
                char c = Peek();
                if (c == delimiter)
                {
                    if (PeekPrev() != '\\')
                        break;
                }

                Advance();
            }

            int sliceIndex = _index;

            Advance();

            if (CheckContext(TokenContext.WithinArray))
                CheckForArrayDeliminter();
            else if (!IsDeliminationCapable(Peek()))
                ThrowParseException("Expected empty character or EOF after string");

            _lastReadToken = SDFTokenType.String;
            _slice = _source.Slice(previous, sliceIndex - previous);

            return true;
        }

        private ReadOnlySpan<char> ReadIdentifier(bool includePreviousToken = false)
        {
            int previous = includePreviousToken ? _index - 1 : _index;
            if (previous < 0)
                ThrowParseException("Identifier previous index is less than 0");

            while (true)
            {
                char c = Peek();
                if (!IsIdentifierCapable(c))
                    break;

                Advance();
            }

            return _source.Slice(previous, _index - previous);
        }
        #endregion

        public void Reset()
        {
            _lastReadToken = SDFTokenType.Unknown;

            _line = 0;
            _index = 0;

            _context.Clear();
        }

        public bool Read()
        {
            if (IsCharSkippable(Peek()))
            {
                Advance();

                while (!IsEOF && IsCharSkippable(Peek())) Advance();
            }

            return TryParseNextToken();
        }

        public bool Skip() => throw new NotImplementedException();

        public SDFTokenType TokenType => _lastReadToken;

        public int Index { get => _index; set => _index = value; }
        public int Line { get => _line; set => _line = value; }

        public ReadOnlySpan<char> Slice => _slice;

        private static bool IsCharSkippable(char c) => char.IsControl(c) || char.IsWhiteSpace(c);

        private enum TokenContext : byte
        {
            None = 0,

            WithinObject,
            WithinArray
        }
    }

    public enum SDFTokenType : byte
    {
        Unknown = 0,

        Property,

        ObjectBegin,
        ObjectEnd,

        ArrayBegin,
        ArrayEnd,

        Number,
        String,
        Boolean
    }

    public class SDFParseException : Exception
    {
        public SDFParseException()
        {
        }

        public SDFParseException(string? message) : base(message)
        {
        }

        public SDFParseException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected SDFParseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
