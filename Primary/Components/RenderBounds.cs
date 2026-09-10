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
        public AABB ComputedBounds = AABB.Zero;
        public int UpdateIndex = -1;
        public int MeshLoadIndex = -1;

        public RenderBounds()
        {
        }
    }
}
