using Primary.Rendering.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components
{
    internal struct RenderTreeAdditionalInfo : IComponent
    {
        public TreeRegion TreeRegion;
        public TreeRegion NodeRegion;
    }
}
