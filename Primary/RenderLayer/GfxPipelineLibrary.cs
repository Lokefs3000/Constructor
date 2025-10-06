using Primary.Common;

namespace Primary.RenderLayer
{
    public record struct GfxPipelineLibrary : IDisposable
    {
        private RHI.PipelineLibrary? _internal;

        public GfxPipelineLibrary() => throw new NotSupportedException();
        internal GfxPipelineLibrary(RHI.PipelineLibrary? piplineLibrary) => _internal = piplineLibrary;

        public void Dispose() => _internal?.Dispose();

        #region Base

        public void GetPipelineLibraryData(Span<byte> data) => NullableUtility.ThrowIfNull(_internal).GetPipelineLibraryData(data);

        #endregion

        public long PipelineDataSize => NullableUtility.ThrowIfNull(_internal).PipelineDataSize;

        public bool IsNull => _internal == null;
        public RHI.PipelineLibrary? RHIPipelineLibrary => _internal;

        public static GfxPipelineLibrary Null = new GfxPipelineLibrary(null);

        public static explicit operator RHI.PipelineLibrary?(GfxPipelineLibrary piplineLibrary) => piplineLibrary._internal;
        public static implicit operator GfxPipelineLibrary(RHI.PipelineLibrary? piplineLibrary) => new GfxPipelineLibrary(piplineLibrary);
    }
}
