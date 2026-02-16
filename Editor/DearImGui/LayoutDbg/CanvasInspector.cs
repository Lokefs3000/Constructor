using Editor.UI.Elements;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.DearImGui.LayoutDbg
{
    internal class CanvasInspector : IElementInspector
    {
        public void Inspect(UIElement element)
        {
            UICanvas canvas = Unsafe.As<UICanvas>(element);

            Vector2 clientOffset = canvas.ClientOffset;
            Vector2 clientSize = canvas.ClientSize;

            if (ImGui.DragFloat2("Client offset"u8, ref Unsafe.As<Vector2, float>(ref clientOffset)))
                canvas.ClientOffset = clientOffset;
            if (ImGui.DragFloat2("Client size"u8, ref Unsafe.As<Vector2, float>(ref clientSize)))
                canvas.ClientSize = clientSize;
        }
    }
}
