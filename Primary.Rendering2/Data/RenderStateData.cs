using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Data
{
    public sealed class RenderStateData : IContextItem
    {
        public IRenderPath Path { get; internal set; }

        internal RenderStateData()
        {

        }
    }
}
