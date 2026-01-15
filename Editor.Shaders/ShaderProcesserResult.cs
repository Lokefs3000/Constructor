using Editor.Shaders.Data;

namespace Editor.Shaders
{
    public readonly record struct ShaderProcesserResult(ushort TargetData, ShaderBytecode[] Bytecodes, string[] IncludedFiles, ShaderData Data)
    {
        public ShaderCompileTarget Targets => (ShaderCompileTarget)(TargetData & 0x3);
        public ShaderCompileStage Stages => (ShaderCompileStage)((TargetData >> 8) & 0x3fff);
    }

    public readonly record struct ShaderBytecode(ushort TargetData, byte[] Bytes)
    {
        public ShaderCompileTarget Target => (ShaderCompileTarget)(TargetData & 0x7);
        public ShaderCompileStage Stage => (ShaderCompileStage)((TargetData >> 8) & 0x3fff);
        public int Index => (int)((TargetData >> 3) & 0xf);
    }
}
