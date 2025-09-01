using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RenderLayer
{
    public record struct GfxTexture : IDisposable
    {
        private RHI.Texture? _internal;

        public GfxTexture() => throw new NotSupportedException();
        internal GfxTexture(RHI.Texture? texture) => _internal = texture;

        public void Dispose() => _internal?.Dispose();

        #region Base



        #endregion

        public nint Handle => _internal?.Handle ?? nint.Zero;

        public ref readonly RHI.TextureDescription Description => ref NullableUtility.ThrowIfNull(_internal).Description;
        public string Name { set => NullableUtility.ThrowIfNull(_internal).Name = value; }

        public bool IsNull => _internal == null;
        public RHI.Texture? RHITexture => _internal;
        public GfxResource Resource => new GfxResource(_internal);

        public static GfxTexture Null = new GfxTexture(null);

        public static explicit operator RHI.Texture?(GfxTexture texture) => texture._internal;
        public static implicit operator GfxTexture(RHI.Texture? texture) => new GfxTexture(texture);

        public static explicit operator GfxResource(GfxTexture texture) => new GfxResource(texture._internal);
    }
}
