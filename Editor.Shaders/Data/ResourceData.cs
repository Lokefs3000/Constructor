using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Data
{
    public readonly record struct ResourceData(string Name, ResourceType Type, ValueDataRef Value, AttributeData[] Attributes, IndexRange DeclerationRange);
}
