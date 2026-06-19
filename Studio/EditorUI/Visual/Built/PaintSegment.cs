using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Visual.Draw;
using Primary.Common;
using Primary.Mathematics;

namespace EditorUI.Visual.Built
{
    public readonly record struct PaintSegment(PaintCmdType CmdType, IndexRange IndexRange, object? Argument, Rect? ClipRect);
}
