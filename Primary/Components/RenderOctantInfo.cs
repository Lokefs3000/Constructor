using Primary.Editor;
using Primary.Mathematics;
using Primary.Rendering.Tree;

namespace Primary.Components
{
    [Component, DontSerializeComponent]
    [InspectorHidden]
    internal struct RenderOctantInfo : IComponent
    {
        public RenderOctree? Octree;
        public int RenderableId;

        public long LastUpdateFrame;

        public RenderOctantInfo()
        {
            Octree = null;
            RenderableId = -1;

            LastUpdateFrame = -1;
        }
    }
}
