using System;
using System.Collections.Generic;
using System.Text;

namespace NewShaderProcessor.Lexer
{
    internal ref struct SourceReader
    {
        private readonly ReadOnlySpan<char> _source;

        private int _index;
        private int _line;

        private int _lineStartIndex;

        internal SourceReader(ReadOnlySpan<char> source)
        {
            _source = source;

            _index = 0;
            _line = 0;

            _lineStartIndex = 0;
        }
    }

    internal enum SourceToken : byte
    {
        EOF = 0,

        Identifier,
        Number,

        Comma,      // ,
        Period,     // .
        Semicolon,  // ;
        Colon,      // :
        Equals,     // =

        True,
        False,

        ParenOpen,
        ParenClose,
        SquareParanOpen,
        SquareParanClose,
        CurlyParanOpen,
        CurlyParanClose,

        Struct,

        Static,

    }
}
