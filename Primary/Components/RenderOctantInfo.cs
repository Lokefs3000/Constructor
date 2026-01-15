using Primary.Rendering.Tree;

namespace Primary.Components
{
    internal struct RenderOctantInfo : IComponent
    {
        public OctreePoint Tree;
        public int OctantId;
    }
}
