using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;

namespace NewShaderProcessor.Lexer
{
    public abstract class TokenNode
    {
        private readonly int _sourceLine;
        private readonly IndexRange _sourceRange;

        internal TokenNode(int sourceLine, IndexRange sourceRange)
        {
            _sourceLine = sourceLine;
            _sourceRange = sourceRange;
        }
    }
}
