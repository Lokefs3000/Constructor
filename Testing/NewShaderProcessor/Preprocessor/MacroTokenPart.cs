using System;
using System.Collections.Generic;
using System.Text;

namespace NewShaderProcessor.Preprocessor
{
    internal readonly record struct MacroTokenPart(TokenPartType Type, string? TokenPart = null, int ArgumentIndex = -1);

    internal enum TokenPartType : byte
    {
        Plain = 0,
        Argument,
        Stringizing,
        Charizing,
        LineBreak
    }
}
