using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.Tooling
{
    internal sealed class CubemapTool
    {
        private bool _isVisible;

        internal CubemapTool()
        {
            _isVisible = false;
        }

        internal void Render()
        {
            if (_isVisible)
            {
                if (ImGui.Begin("Cubemap tool"u8, ref _isVisible))
                {
                    
                }
                ImGui.End();
            }
        }
    }
}
