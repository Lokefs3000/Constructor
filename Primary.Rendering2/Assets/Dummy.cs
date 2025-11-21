using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Assets
{
    internal readonly record struct ShaderProperty(string Name, int Index, ShaderPropertyVisiblity Visiblity, ShaderPropertyType Type, ShaderPropertyDefault Default, ShaderPropertyStages Stages);

    internal enum ShaderPropertyType : byte
    {
        Buffer = 0,
        Texture,

        Single,
        Double,
        UInt32,

        Vector2,
        Vector3,
        Vector4,

        Matrix4x4
    }

    internal enum ShaderPropertyDefault : byte
    {
        None = 0,

        NumOne,
        NumZero,
        NumIdentity,

        TexWhite,
        TexBlack,
        TexNormal,
        TexMask
    }

    internal enum ShaderPropertyVisiblity : byte
    {
        Public = 0,
        Global
    }

    internal enum ShaderPropertyStages : byte
    {
        None = 0,

        GenericShader = 1 << 0,
        PixelShader = 1 << 1
    }
}
