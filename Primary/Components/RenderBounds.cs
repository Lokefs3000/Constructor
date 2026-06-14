using Primary.Common;
using Primary.Mathematics;
using Primary.Editor;

namespace Primary.Components
{
    [Component, DontSerializeComponent]
    [ComponentUsage(CanBeAdded: false)]
    [InspectorHidden]
    public struct RenderBounds : IComponent
    {
        public AABB ComputedBounds;
        public int UpdateIndex;
    }
}
