using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI
{
    public interface IWindowDockHost : IWindowHost
    {
        public IWindowDockHost? ParentHost { get; }

        public ReadOnlySpan<IWindow> Windows { get; }
        public ReadOnlySpan<IWindowHost> Hosts { get; }

        public void DockNewWindow(IWindow window);
        public void UndockWindow(IWindow window);

        public void FocusWindow(IWindow window);
    }
}
