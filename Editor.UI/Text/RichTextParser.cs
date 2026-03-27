using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.UI.Text
{
    public ref struct RichTextParser : IEnumerator<ReadOnlySpan<char>>, IEnumerable<ReadOnlySpan<char>>
    {
        private ReadOnlySpan<char> _span;
        private int _index;

        private ReadOnlySpan<char> _current;

        public RichTextParser(ReadOnlySpan<char> span)
        {
            _span = span;
            _index = span.IsEmpty || span[0] != '<' ? -1 : 1;
        }

        public void Dispose()
        {
            _span = ReadOnlySpan<char>.Empty;
            _index = -1;

            _current = ReadOnlySpan<char>.Empty;
        }

        public void Reset()
        {
            _index = _span.IsEmpty || _span[0] != '<' ? -1 : 1;

            _current = ReadOnlySpan<char>.Empty;
        }

        public bool MoveNext()
        {
            if (_index == -1)
                return false;

            if (char.IsWhiteSpace(_span[_index]))
            {
                do
                {
                    if (++_index == _span.Length)
                    {
                        _current = ReadOnlySpan<char>.Empty;
                        _index = -1;

                        return false;
                    }
                } while (char.IsWhiteSpace(_span[_index]));
            }

            char c = _span[_index];
            if (c == '>')
            {
                _current = _span.Slice(_index, 1);
                _index = -1;

                return true;
            }
            else if (c == '=')
            {
                if (_span.Length < _index + 2)
                    goto EndParse;
                if (_span[++_index] != '"')
                    goto EndParse;

                int start = _index;
                do { ++_index; } while (_index < _span.Length && _span[_index] != '"');

                if (_index == _span.Length || _span[_index] != '"')
                    goto EndParse;

                _current = _span.Slice(start + 1, _index++ - start - 1);
                return true;
            }
            else if (char.IsLetter(c) || c == '/')
            {
                int start = _index;
                do { ++_index; } while (_index < _span.Length && char.IsLetter(_span[_index]));

                if (_index == _span.Length)
                    goto EndParse;

                _current = _span.Slice(start, _index - start);
                return true;
            }

        EndParse:
            _current = ReadOnlySpan<char>.Empty;
            _index = -1;

            return false;
        }

        IEnumerator<ReadOnlySpan<char>> IEnumerable<ReadOnlySpan<char>>.GetEnumerator() => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

        public ReadOnlySpan<char> Current => _current;
        [Obsolete]
        object IEnumerator.Current => Current.ToString();

        public int Index => _index;
    }

    public enum RTValueType : byte
    {
        BeginElement,
        EndElement,

        Value
    }
}
