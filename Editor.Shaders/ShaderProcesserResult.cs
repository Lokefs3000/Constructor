using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Shaders
{
    public readonly record struct ShaderProcesserResult(ushort TargetData, ShaderBytecode[] Bytecodes, string[] IncludedFiles)
    {
        public ShaderCompileTarget Targets => (ShaderCompileTarget)(TargetData & 0x3);
        public ShaderCompileStage Stages => (ShaderCompileStage)((TargetData >> 2) & 0x3fff);
    }

    public readonly record struct ShaderBytecode(ushort TargetData, byte[] Bytes)
    {
        public ShaderCompileTarget Target => (ShaderCompileTarget)(TargetData & 0x3);
        public ShaderCompileStage Stage => (ShaderCompileStage)((TargetData >> 2) & 0x3fff);
    }
}
