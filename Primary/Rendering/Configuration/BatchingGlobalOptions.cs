using System;
using System.Collections.Generic;
using System.Text;
using Primary.Console;

namespace Primary.Rendering.Configuration
{
    [ConsoleName("r.batch")]
    public sealed class BatchingGlobalOptions
    {
        [ConsoleName("frustrumCull", description: "Cull chunks and renderables outside the viewing frustrum")]
        public bool FrustrumCulling = true;

        [ConsoleName("forceFrustrumCull", description: "Disallow skipping frustrum cull for renderables if chunk is fully within frustrum")]
        public bool ForceFrustrumCull = false;

        [ConsoleName("threadedCull", description: "Split chunk culling over multiple threads at once")]
        public bool ThreadedCulling = true;

        public static BatchingGlobalOptions Instance => Engine.GlobalSingleton.RenderingManager.BatchingManager.Options;
    }
}
