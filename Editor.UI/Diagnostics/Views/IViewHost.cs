using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Diagnostics.Views
{
    internal interface IViewHost
    {
        public void Initialize();
        public void Cleanup();
    }
}
