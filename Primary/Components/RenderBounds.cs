using Primary.Common;
using Primary.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
