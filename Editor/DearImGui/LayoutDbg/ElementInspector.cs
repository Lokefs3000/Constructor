using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Hexa.NET.ImGui;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.DearImGui.LayoutDbg
{
    internal class ElementInspector : IElementInspector
    {
        public void Inspect(UIElement element)
        {
            UIValue2 position = element.Position;
            UIValue2 size = element.Size;

            if (IElementInspector.InputUIValue2("Position"u8, ref position))
                element.Position = position;
            if (IElementInspector.InputUIValue2("Size"u8, ref size))
                element.Size = size;
        }
    }
}
