using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RHI
{
    //TODO: add mip map generation support
    public abstract class RenderTarget : Resource
    {
        public abstract ref readonly RenderTargetDescription Description { get; }
        public abstract string Name { set; }

        public abstract RenderTextureView? ColorTexture { get; }
        public abstract RenderTextureView? DepthTexture { get; }
    }

    public abstract class RenderTextureView : Resource
    {
        public abstract string Name { set; }

        public abstract bool IsShaderVisible { get; }
    }

    public record struct RenderTargetDescription
    {
        public Size Dimensions;

        public RenderTargetFormat ColorFormat;
        public DepthStencilFormat DepthFormat;

        public RenderTargetVisiblity ShaderVisibility;
    }

    [Flags]
    public enum RenderTargetVisiblity : byte
    {
        None = 0,

        Color = 1 << 0,
        Depth = 1 << 1,

        Both = Color | Depth
    }
}
