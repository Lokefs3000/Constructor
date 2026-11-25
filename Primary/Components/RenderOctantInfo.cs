using Primary.Editor;
using Primary.Mathematics;
using Primary.Rendering.Partitioning;

namespace Primary.Components
{
    [Component, DontSerializeComponent]
    [InspectorHidden]
    internal struct RenderOctantInfo : IComponent
    {
        public RenderGridChunk? Chunk;
        public int RenderableId;

        public long LastUpdateFrame;

        public RenderOctantInfo()
        {
            Chunk = null;
            RenderableId = -1;

            LastUpdateFrame = -1;
        }
    }
}
