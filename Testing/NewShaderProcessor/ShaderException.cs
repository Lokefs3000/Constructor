using System;
using System.Collections.Generic;
using System.Text;

namespace NewShaderProcessor
{
    public class ShaderException : Exception
    {
        private readonly SourceLocation _location;

        public ShaderException(SourceLocation location, string? message) : base($"[{location}]: {message}")
        {
            _location = location;
        }

        public SourceLocation Location => _location;
    }
}
