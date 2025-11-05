using Primary.Rendering2;
using Primary.Rendering2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.R2.ForwardPlus.Passes
{
    internal sealed class OpaquePass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderStateData stateData = context.Get<RenderStateData>()!;
            ForwardPlusRenderPath renderPath = Unsafe.As<ForwardPlusRenderPath>(stateData.Path);

            //using (RasterPassDescription desc = renderPass.SetupRasterPass("Opaque"))
            //{
            //    
            //}
        }
    }
}
