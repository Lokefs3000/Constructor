using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets;
using Primary.Rendering.Assets;

namespace Primary.Rendering.Batching
{
    public readonly record struct MaterialPass(ulong PassId, MaterialAsset Material, ShaderAsset Shader, ROPropertyBlock Properties)
    {
        public static MaterialShaderType GetShaderType(ulong passId) => (MaterialShaderType)((passId >> 48) & 0xff);
        public static int GetShaderIndex(ulong passId) => (int)((passId >> 24) & 0xffffff);
        public static int GetMaterialIndex(ulong passId) => (int)(passId & 0xffffff);
    }
}
