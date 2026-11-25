using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Resources;
using Primary.Threading;

namespace Primary.Rendering.Playback
{
    internal interface IPlaybackDevice : IDisposable
    {
        public JobHandle ExecuteRenderPlan(CompiledRenderPlan renderPlan);

        public ResourceAlignmentInfo QueryForAlignment(in FGBufferDesc bufferDesc);
        public ResourceAlignmentInfo QueryForAlignment(in FGTextureDesc textureDesc);
    }
}
