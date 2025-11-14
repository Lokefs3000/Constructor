using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Data
{
    public readonly record struct FunctionData(string Name, AttributeData[] Attributes, Range BodyRange);
}
