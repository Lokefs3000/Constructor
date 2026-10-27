using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace NewShaderProcessor.Preprocessor
{
    internal readonly record struct DefineData(ImmutableArray<string> Arguments, ImmutableArray<MacroTokenPart> TokenParts);
}
