using Primary.Common;

namespace Primary.RenderLayer
{
    public record struct GfxBuffer : IDisposable
    {
        private RHI.Buffer? _internal;

        public GfxBuffer() => throw new NotSupportedException();
        internal GfxBuffer(RHI.Buffer? buffer) => _internal = buffer;

        public void Dispose() => _internal?.Dispose();

        #region Base



        #endregion

        public nint Handle => NullableUtility.ThrowIfNull(_internal).Handle;

        public ref readonly RHI.BufferDescription Description => ref NullableUtility.ThrowIfNull(_internal).Description;
        public string Name { set => NullableUtility.ThrowIfNull(_internal).Name = value; }

        public bool IsNull => _internal == null;
        public RHI.Buffer? RHIBuffer => _internal;
        public GfxResource Resource => new GfxResource(_internal);

        public static GfxBuffer Null = new GfxBuffer(null);

        public static explicit operator RHI.Buffer?(GfxBuffer buffer) => buffer._internal;
        public static implicit operator GfxBuffer(RHI.Buffer? buffer) => new GfxBuffer(buffer);

        public static explicit operator GfxResource(GfxBuffer buffer) => new GfxResource(buffer._internal);
    }
}
