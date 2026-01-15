using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Rendering.Resources
{
    public static class FGResourceUtility
    {
        public static uint GetWidth(FrameGraphBuffer buffer) => buffer.IsExternal ? Unsafe.As<RHIBuffer>(buffer.Resource!).Description.Width : buffer.Description.Width;
        
        public static (int, int, int) GetTextureSize(FrameGraphTexture texture)
        {
            if (texture.IsExternal)
            {
                RHITexture res = Unsafe.As<RHITexture>(texture.Resource!);
                return (res.Description.Width, res.Description.Height, res.Description.Dimension == RHIDimension.Texture3D ? res.Description.Depth : 1);
            }
            else
            {
                return (texture.Description.Width, texture.Description.Height, texture.Description.Dimension == FGTextureDimension._3D ? texture.Description.Depth : 1);
            }
        }

        public static (int, int, int) GetSizeForSubresource(uint subresource, int width, int height, int depth)
        {
            ++subresource;
            return ((int)(width / subresource), (int)(height / subresource), (int)(depth / subresource));
        }
    
        public static int GetMaxSubresources(FrameGraphResource resource)
        {
            if (resource.ResourceId == FGResourceId.Buffer)
                return 1;

            return resource.IsExternal ? Unsafe.As<RHITexture>(resource.Resource!).Description.MipLevels : 1;
        }

        public static RHIFormat GetFormat(FrameGraphTexture texture) => texture.IsExternal ? Unsafe.As<RHITexture>(texture.Resource!).Description.Format : texture.Description.Format;
    }
}
