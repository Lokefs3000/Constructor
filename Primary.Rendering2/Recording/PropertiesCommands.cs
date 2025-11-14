using Primary.Rendering2.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    internal enum SetPropertyType : byte
    {
        Buffer = 0,
        Texture,
        RHIBuffer,
        RHITexture
    }

    internal enum SetPropertyTarget : byte
    {
        GenericShader = 0,
        PixelShader
    }

    internal struct PropertyMeta
    {
        public SetPropertyType Type;
        public ShaderPropertyStages Target;
    }

    internal struct UnamangedPropertyData
    {
        public PropertyMeta Meta;
        public nint Resource;
    }
}
