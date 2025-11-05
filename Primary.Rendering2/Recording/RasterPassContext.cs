using Primary.Rendering2.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Recording
{
    public sealed class RasterPassContext
    {
        internal RasterPassContext()
        {

        }

        public RasterCommandBuffer CommandBuffer => new RasterCommandBuffer();
    }
}
