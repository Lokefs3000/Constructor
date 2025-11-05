using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Resources
{
    public readonly struct FrameGraphTexture
    {
        private readonly FrameGraphResource _resource;

        internal FrameGraphTexture(FrameGraphResource resource)
        {
            _resource = resource;
        }

        public FrameGraphTextureDesc Description => _resource.TextureDesc;
        public int Index => _resource.Index;

        public static readonly FrameGraphTexture Invalid = new FrameGraphTexture(new FrameGraphResource(-1, default(FrameGraphTextureDesc)));

        public static implicit operator FrameGraphResource(FrameGraphTexture resource) => resource._resource;
        public static explicit operator FrameGraphTexture(FrameGraphResource resource) => resource.AsTexture();
    }
}
