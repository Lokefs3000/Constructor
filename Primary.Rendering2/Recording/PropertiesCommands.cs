using Primary.Assets;
using Primary.Rendering2.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    public enum SetPropertyType : byte
    {
        None = 0,
        Buffer,
        Texture,
        RHIBuffer,
        RHITexture,
        RHISampler
    }

    public enum SetPropertyTarget : byte
    {
        GenericShader = 0,
        PixelShader
    }

    public struct PropertyMeta
    {
        public SetPropertyType Type;
        public ShPropertyStages Target;
    }

    public struct UnmanagedPropertyData
    {
        public PropertyMeta Meta;
        public bool IsExternal;
        public nint Resource;
    }
}
