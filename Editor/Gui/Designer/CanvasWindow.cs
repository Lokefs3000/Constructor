using Editor.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Designer
{
    internal sealed class CanvasWindow : UIWindow
    {
        public CanvasWindow(int uniqueWindowId) : base(uniqueWindowId)
        {
            WindowTitle = "UIDesigner Canvas Window";
        }
    }
}
