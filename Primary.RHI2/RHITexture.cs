namespace Primary.RHI2
{
    public unsafe abstract class RHITexture : RHIResource, IAsNativeObject<RHITextureNative>
    {
        protected RHITextureDescription _description;

        public ref readonly RHITextureDescription Description => ref _description;

        public abstract RHITextureNative* GetAsNative();
    }

    public struct RHITextureNative
    {
        public RHITextureDescription Description;
    }

    public struct RHITextureDescription
    {
        public int Width;
        public int Height;
        public int DepthOrArraySize;

        public int MipLevels;

        public RHIResourceUsage Usage;
        public RHIDimension Dimension;
        public RHIFormat Format;

        public RHISwizzle Swizzle;

        public RHITextureDescription()
        {
            Width = 0;
            Height = 0;
            DepthOrArraySize = 0;

            MipLevels = 0;

            Usage = RHIResourceUsage.None;
            Dimension = RHIDimension.Texture1D;
            Format = RHIFormat.Unknown;

            Swizzle = RHISwizzle.RGBA;
        }

        public RHITextureDescription(in RHITextureDescription other)
        {
            Width = other.Width;
            Height = other.Height;
            DepthOrArraySize = other.DepthOrArraySize;

            MipLevels = other.MipLevels;

            Usage = other.Usage;
            Dimension = other.Dimension;
            Format = other.Format;

            Swizzle = other.Swizzle;
        }

        public int Depth { get => DepthOrArraySize; set => DepthOrArraySize = value; }
        public int ArraySize { get => DepthOrArraySize; set => DepthOrArraySize = value; }
    }

    public struct RHISwizzle
    {
        public ushort Swizzle;

        public RHISwizzle(ushort swizzle)
        {
            Swizzle = swizzle;
        }

        public RHISwizzle(RHISwizzleChannel r, RHISwizzleChannel g, RHISwizzleChannel b, RHISwizzleChannel a = RHISwizzleChannel.Alpha)
        {
            Swizzle = (ushort)(((int)r << SwizzleRAmount) | ((int)g << SwizzleGAmount) | ((int)b << SwizzleGAmount) | ((int)a << SwizzleAAmount));
        }

        public RHISwizzleChannel R
        {
            get => (RHISwizzleChannel)((Swizzle >> SwizzleRAmount) & SwizzleMask);
            set => Swizzle = (ushort)((Swizzle & ~(SwizzleMask << SwizzleRAmount)) | ((int)value << SwizzleRAmount));
        }

        public RHISwizzleChannel G
        {
            get => (RHISwizzleChannel)((Swizzle >> SwizzleGAmount) & SwizzleMask);
            set => Swizzle = (ushort)((Swizzle & ~(SwizzleMask << SwizzleGAmount)) | ((int)value << SwizzleGAmount));
        }

        public RHISwizzleChannel B
        {
            get => (RHISwizzleChannel)((Swizzle >> SwizzleBAmount) & SwizzleMask);
            set => Swizzle = (ushort)((Swizzle & ~(SwizzleMask << SwizzleBAmount)) | ((int)value << SwizzleBAmount));
        }

        public RHISwizzleChannel A
        {
            get => (RHISwizzleChannel)((Swizzle >> SwizzleAAmount) & SwizzleMask);
            set => Swizzle = (ushort)((Swizzle & ~(SwizzleMask << SwizzleAAmount)) | ((int)value << SwizzleAAmount));
        }

        public static RHISwizzle RGBA = new RHISwizzle(RHISwizzleChannel.Red, RHISwizzleChannel.Green, RHISwizzleChannel.Blue);

        public const int SwizzleMask = 0x7;
        public const int SwizzleRAmount = 9;
        public const int SwizzleGAmount = 6;
        public const int SwizzleBAmount = 3;
        public const int SwizzleAAmount = 0;
    }
}
