using Primary.Assets;
using Primary.Rendering.Assets;

namespace Primary.Rendering.Recording
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

    public enum SetPropertyFlags : byte
    {
        None = 0,

        UnorderedAccess = 1 << 0
    }

    public struct PropertyMeta
    {
        public SetPropertyType Type;
        public ShPropertyStages Target;
        public SetPropertyFlags Flags;
    }

    public struct UnmanagedPropertyData
    {
        public PropertyMeta Meta;
        public bool IsExternal;
        public nint Resource;
    }
}
