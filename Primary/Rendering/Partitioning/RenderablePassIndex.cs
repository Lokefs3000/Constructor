using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Partitioning
{
    public readonly record struct RenderablePassIndex(ulong PassId, int ArrayIndex);
}
