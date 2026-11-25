using System;
using System.Collections.Generic;
using System.Text;
using Editor.UI;

namespace Editor.Inspector.V2.Diagnostics
{
    internal sealed class EntityDebugWindow : UIWindow
    {
        public EntityDebugWindow(int uniqueWindowId) : base(uniqueWindowId)
        {
            WindowTitle = "Entity inspector system debugger";
        }
    }
}
