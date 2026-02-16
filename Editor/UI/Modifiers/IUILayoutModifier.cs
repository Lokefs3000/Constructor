using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Modifiers
{
    public interface IUILayoutModifier
    {
        public void MeasureSize(Vector2 treeSize);
        public void ModifyElement(ref UIMeasurements measurements);
        public void DrawVisual(UICommandBuffer commandBuffer);
    }

    public enum UILayoutModiferTime : byte
    {
        Ascending = 1 << 0,
        Descending = 1 << 1,
    }

    public enum UILayoutContext : byte
    {
        BeforeRecalc = 1 << 0,
        AfterRecalc = 1 << 1,
        AfterChildrenRecalc = 1 << 2
    }
}
