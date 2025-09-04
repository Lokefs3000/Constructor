using Primary.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Rendering
{
    internal sealed class RenderPathManager
    {
        private IRenderPath _primary;

        internal RenderPathManager(RenderingManager rendering)
        {
            _primary = rendering.RenderPath;
        }


    }

}
