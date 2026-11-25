using System;
using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;

namespace VoxelizationDemo.Editor.Windows.Snippets
{
    internal abstract class SnippetView
    {
        internal bool UpdateAndRender()
        {
            bool isOpen = true;
            if (ImGui.Begin(GetType().Name, ref isOpen, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
            {
                OnDrawContents();

                if (ImGui.BeginPopupContextWindow())
                {
                    OnDrawContextMenu();
                    ImGui.EndPopup();
                }
            }
            ImGui.End();

            return isOpen;
        }

        protected virtual void OnDrawContents()
        {
        }

        protected virtual void OnDrawContextMenu()
        {
        }
    }
}
