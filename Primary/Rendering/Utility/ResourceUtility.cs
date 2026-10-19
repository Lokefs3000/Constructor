using System;
using System.Collections.Generic;
using System.Text;
using Primary.Mathematics;
using Primary.RHI;

namespace Primary.Rendering.Utility
{
    public static class ResourceUtility
    {
        public static Int3 CalculateSizeForSlice(in FGTextureDesc desc, int slice)
        {
            int mipIndex = desc.Dimension switch
            {
                FGTextureDimension.Tex2D => slice,
                FGTextureDimension.Tex3D => slice,
                FGTextureDimension.Cube => slice % desc.MipCount,
                FGTextureDimension.Tex2DArray => slice % desc.MipCount,
                FGTextureDimension.CubeArray => slice % desc.MipCount,
                _ => throw new NotImplementedException(),
            };

            Int3 size = Int3.Max(new Int3(desc.Width, desc.Height, desc.DepthOrArraySize) >> mipIndex, Int3.One);
            if (desc.Dimension != FGTextureDimension.Tex3D)
                size.Z = 1;

            return size;
        }

        public static Int3 CalculateSizeForSlice(in RHITextureDescription desc, int slice)
        {
            int mipIndex = desc.Dimension switch
            {
                RHIDimension.Texture1D => slice,
                RHIDimension.Texture2D => slice,
                RHIDimension.Texture3D => slice,
                RHIDimension.TextureCube => slice % desc.MipLevels,
                _ => throw new NotImplementedException(),
            };

            Int3 size = Int3.Max(new Int3(desc.Width, desc.Height, desc.DepthOrArraySize) >> mipIndex, Int3.One);
            if (desc.Dimension != RHIDimension.Texture3D)
                size.Z = 1;

            return size;
        }
    }
}
