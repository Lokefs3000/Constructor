using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.GUI.ImGui
{
    public readonly record struct ImGuiDrawCmd(Vector4 ClipRect, int IndexOffset, RHITexture Texture);
}
