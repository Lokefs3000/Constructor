using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Batching;

namespace Primary.Rendering.Partitioning
{
    public readonly record struct RenderableSlot(int Chunk, int Index);
}
