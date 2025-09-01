using Primary.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components
{
    [InspectorHidden]
    internal record struct EntityName : IComponent
    {
        public string Name;
    }
}
