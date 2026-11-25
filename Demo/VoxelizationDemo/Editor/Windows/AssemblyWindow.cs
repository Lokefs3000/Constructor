using System.Reflection;
using Hexa.NET.ImGui;
using Primary.Scripting;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class AssemblyWindow : IEditorWindow
    {
        public bool UpdateAndRender()
        {
            bool isOpen = true;
            if (ImGui.Begin("Assemblies"u8, ref isOpen, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.MenuItem("Component registry"))
                    {
                        if (!EditorManager.Instance.WindowManager.HasWindowOpen<ComponentRegistryWindow>())
                        {
                            EditorManager.Instance.WindowManager.AddOpenWindow(new ComponentRegistryWindow());
                        }
                    }

                    ImGui.EndMenuBar();
                }

                foreach (ScriptContext context in VoxelRuntime.Instance.ScriptingManager.Contexts)
                {
                    bool headerOpen = ImGui.CollapsingHeader(context.Name, ImGuiTreeNodeFlags.DefaultOpen);
                    if (ImGui.BeginPopupContextItem())
                    {
                        if (ImGui.MenuItem("Reload"))
                        {
                            EditorManager.Instance.ReloadManager.RequestReload();
                        }

                        ImGui.EndPopup();
                    }

                    if (headerOpen)
                    {
                        ImGui.Indent();
                        foreach (Assembly assembly in context.LoadedAssemblies)
                        {
                            ImGui.Selectable(assembly.GetName().ToString());
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
