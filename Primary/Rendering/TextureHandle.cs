using System;
using System.Collections.Generic;
using System.Text;
using Primary.RHI;

namespace Primary.Rendering
{
    public readonly record struct TextureHandle
    {
        private readonly ResourceHandle _handle;

        internal TextureHandle(ResourceHandle handle)
        {
            _handle = handle;
        }

        public override int GetHashCode() => _handle.GetHashCode();

        public readonly int Index => _handle.Index;
        public readonly bool IsImported => _handle.IsImported;

        internal readonly ushort LocalIndex => _handle.LocalIndex;

        public readonly bool IsNull => _handle.IsNull;

        public static readonly TextureHandle Null = new TextureHandle(ResourceHandle.Null);

        public static implicit operator ResourceHandle(TextureHandle handle) => handle._handle;
    }

    public record struct FGTextureDesc
    {
        public string? Name;

        public int Width;
        public int Height;
        public int DepthOrArraySize;

        public int MipCount;

        public FGTextureDimension Dimension;
        public RHIFormat Format;
        public FGTextureTarget Target;

        public FGTextureDesc(int width, int height, RHIFormat format, FGTextureTarget target, string? name = null)
        {
            Name = name;
            Width = width;
            Height = height;
            DepthOrArraySize = 1;

            MipCount = 1;

            Dimension = FGTextureDimension.Tex2D;
            Format = format;
            Target = target;
        }

        public FGTextureDesc(FGTextureDesc textureDesc)
        {
            this = textureDesc;
        }
    }

    public enum FGTextureDimension : byte
    {
        Tex2D,
        Tex3D,
        Cube,
        Tex2DArray,
        CubeArray
    }

    public enum FGTextureTarget : byte
    {
        None = 0,

        RenderTarget,
        DepthStencil,

        RandomReadWrite,
    }
}
