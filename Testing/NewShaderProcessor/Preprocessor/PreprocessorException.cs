using System;
using System.Collections.Generic;
using System.Text;

namespace NewShaderProcessor.Preprocessor
{
    public class PreprocessorException : ShaderException
    {
        public PreprocessorException(SourceLocation location, string? message) : base(location, message)
        {
        }
    }
}
