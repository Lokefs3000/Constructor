using Editor.UI.Elements;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.DearImGui.LayoutDbg
{
    internal sealed class SplitPanelInspector : IElementInspector
    {
        public void Inspect(UIElement element)
        {
            UISplitPanel panel = Unsafe.As<UISplitPanel>(element);

            UISplitDirection direction = panel.Direction;
            float position = panel.Position;

            if (IElementInspector.ComboBox("Direction"u8, ref Unsafe.As<UISplitDirection, int>(ref direction), s_splitDirectionEnum))
                panel.Direction = direction;
            if (ImGui.DragFloat("Position"u8, ref position, 0.025f, 0.0f, 1.0f))
                panel.Position = position;
        }

        private static readonly string[] s_splitDirectionEnum = Enum.GetNames<UISplitDirection>();
    }
}
