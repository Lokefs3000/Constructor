using Editor.UI.Datatypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Elements
{
    public sealed class UIWindowRoot : UIElement
    {
        public UIWindowRoot(IWindow window)
        {
            Position = UIValue2.Zero;
            Size = UIValue2.Max;

            SetNewAndUpdateChildren(window);
        }

        private new void SetParent(UIElement newParent)
        {
        }
    }
}
