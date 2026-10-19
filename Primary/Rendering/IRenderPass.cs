using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering
{
    public interface IRenderPass
    {
        public void SetupPasses(FrameGraphBuilder frameGraph);
    }
}
