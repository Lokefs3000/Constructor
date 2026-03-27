using Editor.UI.Interaction;
using Primary.Mathematics;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI
{
    public interface IWindowHost
    {
        public Int2 ClientOffset { get; }
        public Int2 ClientSize { get; }

        public HostInteractionManager InteractionManager { get; }

        public ReadOnlySpan<UIWindow> Windows { get; }
        public UIWindow? ActiveWindow { get; }

        public RHITexture? HostTexture { get; }

        public void DockNewWindow(UIWindow window);
        public void UndockWindow(UIWindow window);

        public void RecalculateLayout();

        public void TryChangeWindowSize(UIWindow window, Int2 newClientSize);

        public void AddStateFlags(UIStateFlags flags);
    }
}
