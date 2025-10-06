using Primary.Common;

namespace Primary.RenderLayer
{
    public record struct GfxGraphicsPipeline : IDisposable
    {
        private RHI.GraphicsPipeline? _internal;

        public GfxGraphicsPipeline() => throw new NotSupportedException();
        internal GfxGraphicsPipeline(RHI.GraphicsPipeline? graphicsPipeline) => _internal = graphicsPipeline;

        public void Dispose() => _internal?.Dispose();

        #region Base



        #endregion

        public ref readonly RHI.GraphicsPipelineDescription Description => ref NullableUtility.ThrowIfNull(_internal).Description;
        public ref readonly RHI.GraphicsPipelineBytecode Bytecode => ref NullableUtility.ThrowIfNull(_internal).Bytecode;
        public string Name { set => NullableUtility.ThrowIfNull(_internal).Name = value; }

        public bool IsNull => _internal == null;
        public RHI.GraphicsPipeline? RHIGraphicsPipeline => _internal;

        public static GfxGraphicsPipeline Null = new GfxGraphicsPipeline(null);

        public static explicit operator RHI.GraphicsPipeline?(GfxGraphicsPipeline graphicsPipeline) => graphicsPipeline._internal;
        public static implicit operator GfxGraphicsPipeline(RHI.GraphicsPipeline? graphicsPipeline) => new GfxGraphicsPipeline(graphicsPipeline);
    }
}
