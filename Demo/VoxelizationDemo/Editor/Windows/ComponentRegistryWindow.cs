using System;
using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class ComponentRegistryWindow : IEditorWindow
    {
        public bool UpdateAndRender()
        {
            bool isOpen = true;
            if (ImGui.Begin("Component registry"u8, ref isOpen))
            {
                foreach (var (assembly, list) in VoxelRuntime.Instance.SceneManager.EntityManager.Registry.Assemblies)
                {
                    if (ImGui.CollapsingHeader(assembly.GetName().Name, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.Indent();
                        foreach (Type type in list.Types)
                        {
                            ImGui.Selectable(type.FullName);
                        }
                        ImGui.Unindent();
                    }
                }
            }
            ImGui.End();

            return isOpen;
        }
    }
}
