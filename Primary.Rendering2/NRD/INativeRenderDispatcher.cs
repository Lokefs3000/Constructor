using Primary.Rendering2.Pass;
using Primary.Rendering2.Recording;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.NRD
{
    internal interface INativeRenderDispatcher
    {
        public void Dispatch(FrameGraphTimeline timeline, FrameGraphRecorder recorder);
    }
}
