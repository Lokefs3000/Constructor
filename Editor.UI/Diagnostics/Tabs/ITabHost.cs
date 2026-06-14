using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Diagnostics.Tabs
{
    internal interface ITabHost
    {
        public void Initialize();
        public void Cleanup();
    }
}
