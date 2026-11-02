using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Tree
{
    internal readonly record struct RenderableEntitySlot(int Chunk, int IndexInChunk)
    {
        public static readonly RenderableEntitySlot None = new RenderableEntitySlot(-1, -1);
    }
}
