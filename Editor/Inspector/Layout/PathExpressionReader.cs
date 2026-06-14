using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;

namespace Editor.Inspector.Layout
{
    internal ref struct PathExpressionReader
    {
        private ReadOnlySpan<char> _source;
        private int _index;

        private PathExpressionToken _token;
        private IndexRange _readRange;

        private bool _isEof;

        internal PathExpressionReader(ReadOnlySpan<char> source)
        {
            _source = source;
            _index = 0;

            _token = PathExpressionToken.Unknown;
            _readRange = IndexRange.Empty;

            _isEof = false;
        }

        public bool Read()
        {
            if (_index == _source.Length)
            {
                _token = PathExpressionToken.EOF;
                _readRange = IndexRange.Empty;

                _index = -1;
                return true;
            }

            if (_index == -1)
            {
                _token = PathExpressionToken.Unknown;
                _readRange = IndexRange.Empty;

                return false;
            }

            int start = _index;
            char firstChar = _source[_index];

            if (char.IsLetter(firstChar))
            {
                if (_token == PathExpressionToken.Unknown || _token == PathExpressionToken.Deliminator)
                {
                    do
                    {
                        ++_index;
                    } while (_index < _source.Length && char.IsLetterOrDigit(_source[_index]));

                    _token = PathExpressionToken.Name;
                    _readRange = new IndexRange(start, _index);

                    return true;
                }
            }
            else if (firstChar == '[')
            {
                if (_token == PathExpressionToken.Name)
                {
                    do
                    {
                        ++_index;
                    } while (_index < _source.Length && char.IsDigit(_source[_index]));

                    if (_index < _source.Length && _source[_index] == ']')
                    {
                        ++_index;

                        _token = PathExpressionToken.GetItem;
                        _readRange = new IndexRange(start + 1, _index - 1);

                        return true;
                    }
                }
            }
            else if (firstChar == '.')
            {
                if (_token != PathExpressionToken.Deliminator)
                {
                    ++_index;

                    _token = PathExpressionToken.Deliminator;
                    _readRange = new IndexRange(start, _index);

                    return true;
                }
            }

            _index = -1;

            _token = PathExpressionToken.Unknown;
            _readRange = IndexRange.Empty;
            return false;
        }

        public string GetName()
        {
            if (_token != PathExpressionToken.Name)
                throw new InvalidDataException("Current token is not a name");
            return ReadSpan.ToString();
        }

        public int GetItemIndex()
        {
            if (_token != PathExpressionToken.GetItem)
                throw new InvalidDataException("Current token is not get item");
            return int.Parse(ReadSpan);
        }

        public readonly int Index => _index;

        public readonly PathExpressionToken Token => _token;
        public readonly IndexRange ReadRange => _readRange;

        public readonly ReadOnlySpan<char> ReadSpan => _source[(Range)_readRange];
    }

    internal enum PathExpressionToken : byte
    {
        Unknown = 0,

        Name,       // accesing a field/property
        GetItem,    // indexing into an array
        EOF,        // the source has been fully iterated
        Deliminator // deliminator for values '.'
    }
}
