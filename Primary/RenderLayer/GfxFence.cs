using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.RenderLayer
{
    public record struct GfxFence : IDisposable
    {
        private RHI.Fence? _internal;

        public GfxFence() => throw new NotSupportedException();
        internal GfxFence(RHI.Fence? fence) => _internal = fence;

        public void Dispose() => _internal?.Dispose();

        #region Base

        public void Wait(ulong value, RHI.FenceCondition condition, int timeout = -1) => NullableUtility.ThrowIfNull(_internal).Wait(value, condition, timeout);

        #endregion

        public string Name { set => NullableUtility.ThrowIfNull(_internal).Name = value; }

        public ulong CompletedValue => NullableUtility.ThrowIfNull(_internal).CompletedValue;

        public bool IsNull => _internal == null;
        public RHI.Fence? RHIFence => _internal;

        public static GfxFence Null = new GfxFence(null);

        public static explicit operator RHI.Fence?(GfxFence fence) => fence._internal;
        public static implicit operator GfxFence(RHI.Fence? fence) => new GfxFence(fence);
    }
}
