using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using PhysXBindGen.Exceptions;
using Primary.Common;

namespace PhysXBindGen.Parsing
{
    internal ref partial struct TokenReader
    {
        private readonly ReadOnlySpan<char> _source;
        private int _start;
        private int _current;

        private TokenType _token;
        private IndexRange _valueRange;

        private double _numberValue;
        private TokenNumberType _numberType;

        private Stack<BackupState> _backups;

        internal TokenReader(ReadOnlySpan<char> source)
        {
            _source = source;

            _start = 0;
            _current = 0;

            _token = TokenType.Unknown;
            _valueRange = IndexRange.Empty;

            _numberValue = 0.0;
            _numberType = TokenNumberType.Int32;

            _backups = new Stack<BackupState>();
        }

        public void Backup()
        {
            _backups.Push(new BackupState(_start, _current));
        }

        public void Restore()
        {
            if (_backups.TryPop(out BackupState state))
            {
                _start = state.Start;
                _current = state.Current;
            }
        }

        public void Forget()
        {
            _backups.TryPop(out _);
        }

        public bool ReadNext(bool isInterestedInNewLine = false)
        {
            if (IsEOF)
            {
                _token = TokenType.EndOfFile;
                return false;
            }

            _token = TokenType.Unknown;
            _valueRange = IndexRange.Empty;

            _numberValue = 0.0;
            _numberType = TokenNumberType.Int32;

            char c;
            while (char.IsWhiteSpace(c = Advance()) || c == '\n' || c == '\r')
            {
                if (isInterestedInNewLine && c == '\n')
                {
                    _token = TokenType.NewLine;
                    return true;
                }
            }

            _start = _current - 1;

            switch (c)
            {
                case ',':
                    {
                        _token = TokenType.Comma;
                        return true;
                    }
                case ';':
                    {
                        _token = TokenType.Semicolon;
                        return true;
                    }
                case ':':
                    {
                        _token = TokenType.Colon;
                        return true;
                    }
                case '=':
                    {
                        _token = Match('=') ? TokenType.Equality : TokenType.Assignment;
                        return true;
                    }
                case '<':
                    {
                        if (Match('='))
                            _token = TokenType.LessEqual;
                        else
                            _token = Match('<') ? TokenType.BitshiftLeft : TokenType.Less;
                        return true;
                    }
                case '>':
                    {
                        if (Match('='))
                            _token = TokenType.GreaterEqual;
                        else
                            _token = Match('<') ? TokenType.BitshiftRight : TokenType.Greater;
                        return true;
                    }
                case '&':
                    {
                        _token = Match('&') ? TokenType.And : TokenType.BitwiseAnd;
                        return true;
                    }
                case '|':
                    {
                        _token = Match('|') ? TokenType.Or : TokenType.BitwiseOr;
                        return true;
                    }
                case '(':
                    {
                        _token = TokenType.ParanOpen;
                        return true;
                    }
                case ')':
                    {
                        _token = TokenType.ParanClose;
                        return true;
                    }
                case '[':
                    {
                        _token = TokenType.SquareOpen;
                        return true;
                    }
                case ']':
                    {
                        _token = TokenType.SquareClose;
                        return true;
                    }
                case '{':
                    {
                        _token = TokenType.CurlyOpen;
                        return true;
                    }
                case '}':
                    {
                        _token = TokenType.CurlyClose;
                        return true;
                    }
                case '*':
                    {
                        _token = TokenType.Asterix;
                        return true;
                    }
                case '#':
                    {
                        if (Match('#'))
                        {
                            _token = TokenType.MacroOperator;
                            return true;
                        }

                        return ReadPreprocessor();
                    }
                case '/':
                    {
                        return ReadComment();
                    }
            }

            if (char.IsLetter(c))
            {
                return ReadIdentifier();
            }
            else if (char.IsDigit(c) || c == '-' || c == '+')
            {
                return ReadNumber();
            }
            else if (c == '"')
            {
                return ReadString();
            }
            else if (c == '\'')
            {
                return ReadCharacter();
            }

            return false;
        }

        private bool ReadIdentifier()
        {
            while (char.IsLetterOrDigit(Peek()) || Peek() == '_') Advance();

            ReadOnlySpan<char> valueSpan = _source[_start.._current];
            if (valueSpan.SequenceEqual("typedef"))
            {
                _token = TokenType.Typedef;
                return true;
            }
            else if (valueSpan.SequenceEqual("enum"))
            {
                _token = TokenType.Enum;
                return true;
            }
            else if (valueSpan.SequenceEqual("class"))
            {
                _token = TokenType.Class;
                return true;
            }
            else if (valueSpan.SequenceEqual("struct"))
            {
                _token = TokenType.Struct;
                return true;
            }
            else if (valueSpan.SequenceEqual("union"))
            {
                _token = TokenType.Union;
                return true;
            }
            else if (valueSpan.SequenceEqual("const"))
            {
                _token = TokenType.Const;
                return true;
            }
            else if (valueSpan.SequenceEqual("nullptr"))
            {
                _token = TokenType.Nullptr;
                return true;
            }
            else if (valueSpan.SequenceEqual("false"))
            {
                _token = TokenType.BoolFalse;
                return true;
            }
            else if (valueSpan.SequenceEqual("true"))
            {
                _token = TokenType.BoolTrue;
                return true;
            }

            _token = TokenType.Identifier;
            _valueRange = new IndexRange(_start, _current);
            return true;
        }

        private bool ReadNumber()
        {
            bool isHexNumber = false;

            char c;
            while (true)
            {
                if (char.IsDigit(c = Peek()) || c == '.' || c == 'x')
                {
                    if (c == 'x')
                        isHexNumber = true;

                    Advance();
                    continue;
                }

                char lower = char.ToLowerInvariant(c);
                if (isHexNumber && ((lower >= '0' && lower <= '9') || (lower >= 'a' && lower <= 'f')))
                {
                    Advance();
                    continue;
                }

                break;
            }

            ReadOnlySpan<char> valueSpan = _source[_start.._current];
            Guard.IsFalse(valueSpan.IsEmpty);

            NumberStyles styles = isHexNumber ? NumberStyles.HexNumber : NumberStyles.Number;
            if (styles == NumberStyles.HexNumber)
            {
                valueSpan = valueSpan[2..];
            }

            _token = TokenType.Number;
            switch (char.ToLowerInvariant(c))
            {
                case 'f':
                    {
                        Advance();

                        _numberValue = float.Parse(valueSpan, styles, CultureInfo.InvariantCulture);
                        _numberType = TokenNumberType.Single;
                        return true;
                    }
                case 'l':
                    {
                        Advance();

                        _numberValue = long.Parse(valueSpan, styles, CultureInfo.InvariantCulture);
                        _numberType = TokenNumberType.Int64;
                        return true;
                    }
                case 'u':
                    {
                        Advance();

                        if (Match('l'))
                        {
                            _numberValue = ulong.Parse(valueSpan, styles, CultureInfo.InvariantCulture);
                            _numberType = TokenNumberType.UInt64;
                        }
                        else
                        {
                            _numberValue = uint.Parse(valueSpan, styles, CultureInfo.InvariantCulture);
                            _numberType = TokenNumberType.UInt32;
                        }

                        return true;
                    }
                default:
                    {
                        if (valueSpan.Contains('.'))
                        {
                            _numberValue = double.Parse(valueSpan, styles, CultureInfo.InvariantCulture);
                            _numberType = TokenNumberType.Double;
                        }
                        else
                        {
                            _numberValue = int.Parse(valueSpan, styles, CultureInfo.InvariantCulture);
                            _numberType = TokenNumberType.Int32;
                        }

                        return true;
                    }
            }
        }

        private bool ReadString()
        {
            char c;
            do
            {
                if (IsEOF)
                    return false;

                c = Advance();
                if (c == '\n')
                    return false;

            } while (c != '"');

            _token = TokenType.String;
            _valueRange = new IndexRange(_start + 1, _current - 1);
            return true;
        }

        private bool ReadCharacter()
        {
            char c;
            do
            {
                if (IsEOF)
                    return false;

                c = Advance();
                if (c == '\n')
                    return false;

            } while (c != '\'');

            _token = TokenType.Character;
            _valueRange = new IndexRange(_start + 1, _current - 1);
            _numberValue = char.Parse(_source[_valueRange.Start.._valueRange.End].ToString());
            _numberType = TokenNumberType.Char;
            return true;
        }

        private bool ReadPreprocessor()
        {
            while (char.IsLetterOrDigit(Peek())) Advance();

            ReadOnlySpan<char> valueSpan = _source[(_start + 1).._current];
            if (valueSpan.Equals("pragma", StringComparison.Ordinal))
            {
                _token = TokenType.Pragma;
                return true;
            }
            else if (valueSpan.Equals("include", StringComparison.Ordinal))
            {
                _token = TokenType.Include;
                return true;
            }
            else if (valueSpan.Equals("define", StringComparison.Ordinal))
            {
                _token = TokenType.Define;
                return true;
            }

            return false;
        }

        private bool ReadComment()
        {
            char next = Peek();
            if (next == '/')
            {
                Advance();
                while (Peek() != '\n' && !IsEOF) Advance();

                _token = TokenType.Comment;
                _valueRange = new IndexRange(_start + 2, _current);
                return true;
            }
            else if (next == '*')
            {
                Advance();
                while (true)
                {
                    if (IsEOF)
                        return false;
                    if (Peek() == '*' && PeekNext() == '/')
                        break;

                    Advance();
                }

                _token = TokenType.Comment;
                _valueRange = new IndexRange(_start + 2, _current);

                _current += 2;
                return true;
            }

            return false;
        }

        private readonly char Peek() => IsEOF ? '\0' : _source.DangerousGetReferenceAt(_current);
        private readonly char PeekNext() => _current + 1 >= _source.Length ? '\0' : _source.DangerousGetReferenceAt(_current + 1);
        private char Advance() => IsEOF ? '\0' : _source.DangerousGetReferenceAt(_current++);

        private bool Match(char c)
        {
            if (!IsEOF && _source.DangerousGetReferenceAt(_current) == c)
            {
                ++_current;
                return true;
            }

            return false;
        }

        public readonly int Start => _start;
        public readonly int Current => _current;

        public readonly TokenType Token => _token;
        public readonly ReadOnlySpan<char> Value => _source[_valueRange.Start.._valueRange.End];

        public readonly double Number => _numberValue;
        public readonly TokenNumberType NumberType => _numberType;

        public readonly bool IsEOF => _current >= _source.Length;

        private readonly record struct BackupState(int Start, int Current);
    }

    internal ref partial struct TokenReader
    {
        public bool ReadNextToken(TokenType token)
        {
            if (ReadNext() && Token == token)
                return true;

            throw new InvalidTokenException(token, _token);
        }

        public bool TryReadNextToken(TokenType token)
        {
            Backup();
            if (ReadNext() && Token == token)
            {
                Forget();
                return true;
            }

            Restore();
            return false;
        }
    }

    public enum TokenType : byte
    {
        Unknown = 0,
        EndOfFile,
        NewLine,

        Identifier,
        Number,
        String,
        Character,

        Typedef,
        Enum,
        Class,
        Struct,
        Union,
        Const,
        Nullptr,
        BoolFalse,
        BoolTrue,

        Pragma,
        Define,
        Include,

        Comma,
        Semicolon,
        Colon,

        Assignment,

        Equality,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        And,
        Or,

        BitshiftLeft,
        BitshiftRight,
        
        BitwiseOr,
        BitwiseAnd,

        ParanOpen,
        ParanClose,
        SquareOpen,
        SquareClose,
        CurlyOpen,
        CurlyClose,

        Asterix,

        MacroOperator,

        Comment
    }

    public enum TokenNumberType : byte
    {
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        Char
    }
}
