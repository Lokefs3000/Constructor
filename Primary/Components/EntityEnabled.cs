using Primary.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components
{
    [InspectorHidden]
    internal record struct EntityEnabled : IComponent
    {
        public bool Enabled;
    }
}
