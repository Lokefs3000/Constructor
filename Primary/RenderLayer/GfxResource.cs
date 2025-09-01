using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RenderLayer
{
    public record struct GfxResource : IDisposable
    {
        private RHI.Resource? _internal;

        public GfxResource() => throw new NotSupportedException();
        internal GfxResource(RHI.Resource? resource) => _internal = resource;

        public void Dispose() => _internal?.Dispose();

        #region Base



        #endregion

        public nint Handle => NullableUtility.ThrowIfNull(_internal).Handle;

        public bool IsNull => _internal == null;
        public RHI.Resource? RHIResource => _internal;

        public static GfxResource Null = new GfxResource(null);

        public static explicit operator RHI.Resource?(GfxResource resource) => resource._internal;
        public static implicit operator GfxResource(RHI.Resource? resource) => new GfxResource(resource);
    }
}
