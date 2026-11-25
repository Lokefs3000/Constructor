namespace Editor.Shaders
{
    public ref struct ShaderProcessorArgs
    {
        public string InputSource;
        public string? SourceFileName;

        public string[] IncludeDirectories;

        public ShaderCompileTarget Targets;
    }

    public ref struct ComputeShaderProcessorArgs
    {
        public string InputSource;
        public string? SourceFileName;

        public string[] IncludeDirectories;

        public ShaderCompileTarget Targets;
    }

    public enum ShaderCompileTarget : byte
    {
        None = 0,

        Direct3D12 = 1 << 0,
        Vulkan = 1 << 1,
    }

    public enum ShaderCompileStage : byte
    {
        None = 0,

        Vertex = 1 << 0,
        Pixel = 1 << 1,
        Compute = 1 << 2,
    }
}
