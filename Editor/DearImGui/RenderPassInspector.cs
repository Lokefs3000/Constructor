using System;
using System.Collections.Generic;
using System.Text;

using R2 = Primary.Rendering2;

namespace Editor.DearImGui
{
    internal sealed class RenderPassInspector : IDearImGuiWindow
    {
        internal RenderPassInspector()
        {

        }

        public void Render()
        {
            R2.RenderingManager manager = Editor.GlobalSingleton.R2RenderingManager;
        }
    }
}
