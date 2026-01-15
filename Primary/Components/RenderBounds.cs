using Primary.Common;
using Primary.Editor;

namespace Primary.Components
{
    [ComponentUsage(CanBeAdded: false)]
    [InspectorHidden]
    public struct RenderBounds : IComponent
    {
        public AABB ComputedBounds;
        public int UpdateIndex;
    }
}
