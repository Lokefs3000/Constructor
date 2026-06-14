using Editor.UI.Elements;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Visual
{
    public interface IRenderableWindow
    {
        public IWindowHost? ParentHost { get; }

        public Int2 ClientSize { get; }
        public Boundaries InvalidVisualRegion { get; }

        public UIElement RootElement { get; }
    }
}
