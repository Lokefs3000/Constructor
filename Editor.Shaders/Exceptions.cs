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

        public ShaderCompileException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
