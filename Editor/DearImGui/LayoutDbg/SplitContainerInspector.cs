using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.DearImGui.LayoutDbg
{
    internal class SplitContainerInspector : IElementInspector
    {
        public void Inspect(UIElement element)
        {
            UISplitContainer splitContainer = Unsafe.As<UISplitContainer>(element);

            UIColor splitColor = splitContainer.SplitColor;

            if (IElementInspector.InputUIColor("Split color"u8, ref splitColor))
                splitContainer.SplitColor = splitColor;

            if (ImGui.Button("Auto balance splits"u8))
                splitContainer.AutoBalanceSplits(null, true);
        }
    }
}
