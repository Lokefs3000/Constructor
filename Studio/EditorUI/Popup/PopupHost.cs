using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Styling;
using EditorUI.Visual;
using Primary.Windowing;

namespace EditorUI.Popup
{
    internal abstract class PopupHost
    {
        protected internal abstract void Destroy();
        protected internal abstract void Update();
        protected internal abstract void Paint(ref readonly PainterContext painter);

        protected internal abstract StateFlags MenuStateFlags { get; }
    }
}
