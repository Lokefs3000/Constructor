using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Text.Visual;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.RHI;

namespace PrimaryEditor.Rendering.UI
{
    internal sealed class FontTextureFactory : IFontTextureFactory
    {
        public IFontTexture CreateTexture(Int2 size) => new FontTexture(size);

        internal sealed class FontTexture : IFontTexture
        {
            private RHITexture _texture;

            internal FontTexture(Int2 size)
            {
                _texture = CreateTextureFrom(size);
            }

            public void Dispose()
            {
                _texture.Dispose();
            }

            public void Resize(Int2 newSize)
            {
                _texture.Dispose();
                _texture = CreateTextureFrom(newSize);
            }

            private static RHITexture CreateTextureFrom(Int2 size)
            {
                return RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                {
                    Width = size.X,
                    Height = size.Y,
                    DepthOrArraySize = 1,

                    MipLevels = 1,

                    Dimension = RHIDimension.Texture2D,
                    Format = RHIFormat.RGBA8_UNorm,
                    Usage = RHIResourceUsage.ShaderResource,

                    Swizzle = RHISwizzle.RGBA
                }, [], $"FontTexture{size.X}x{size.Y}")!;
            }

            internal RHITexture RawTexture => _texture;
        }
    }
}
