using Primary.Editor;
using Primary.Rendering.Tree;

namespace Primary.Components
{
    [Component, DontSerializeComponent]
    [InspectorHidden]
    internal struct RenderOctantInfo : IComponent
    {
        public OctreePoint Tree;
        public int OctantId;
    }
}
