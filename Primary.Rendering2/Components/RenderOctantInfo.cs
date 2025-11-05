using Primary.Components;
using Primary.Rendering2.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Components
{
    internal struct RenderOctantInfo : IComponent
    {
        public OctreePoint Tree;
        public int OctantId;
    }
}
