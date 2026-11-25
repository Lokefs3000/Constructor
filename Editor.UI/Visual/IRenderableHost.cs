using Primary.Common;
using Primary.Mathematics;
using Primary.RHI;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Visual
{
    public interface IRenderableHost : IInterfaceHost
    {
        public Rect HostMetrics { get; }
        public Rect ContentMetrics { get; }

        public RHITexture? HostTexture { get; }
        public Window? HostWindow { get; }

        public Boundaries InvalidVisualRegion { get; }

        public void DrawVisual(UIPainterContext painter);
    }
}
