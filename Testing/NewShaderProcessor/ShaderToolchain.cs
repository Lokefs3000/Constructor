using System;
using System.Collections.Generic;
using System.Text;
using SharpGen.Runtime;
using Vortice.Dxc;

namespace NewShaderProcessor
{
    public sealed class ShaderToolchain
    {
        public ShaderToolchain()
        {
            
        }

        public void CompileForGraphics(in CompileOptions options)
        {

        }

        private string RunDxcPreprocessor(string sourceText)
        {
            Result r = DxcCompiler.Compiler.Compile(sourceText, )
        }
    }

    public readonly record struct CompileOptions(string SourceText);
}
