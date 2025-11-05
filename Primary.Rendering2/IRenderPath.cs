using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public interface IRenderPath
    {
        public void PreRenderPassSetup(RenderingManager manager);

        public void Install(RenderingManager manager);
        public void Uinstall(RenderingManager manager);
    }
}
