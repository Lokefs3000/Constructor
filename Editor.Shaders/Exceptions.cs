using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Editor.Shaders
{
    public sealed class ShaderCompileException : Exception
    {
        public ShaderCompileException()
        {
        }

        public ShaderCompileException(string? message) : base(message)
        {
        }

        public ShaderCompileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ShaderCompileException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
