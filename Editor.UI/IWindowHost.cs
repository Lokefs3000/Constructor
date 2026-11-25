using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Mathematics;
using Primary.RHI;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI
{
    public interface IWindowHost : IInteractable, IInterfaceHost, IRenderableHost, ILayoutHost
    {
        public HostInteractionManager InteractionManager { get; }

        public IWindow? ActiveWindow { get; }

        public void TryChangeWindowSize(IWindow window, Int2 newClientSize);
    }
}
