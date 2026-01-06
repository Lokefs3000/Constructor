using Primary.Rendering2.Pass;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.NRD
{
    public interface INativeRenderDispatcher
    {
        public void Dispatch(RenderPassManager manager);

        public NRDResourceInfo QueryResourceInfo(FrameGraphResource resource);
        public NRDResourceInfo QueryBufferInfo(FrameGraphBuffer buffer, int offset, int size);
    }

    public readonly record struct NRDResourceInfo(int SizeInBytes, int Alignment);
}
