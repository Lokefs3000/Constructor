using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Menu
{
    public interface IContextMenuListener
    {
        public bool OnItemPressed(ContextMenuBase item);
    }
}
