using Editor.Assets;
using Editor.Assets.Loaders;
using Editor.Assets.Types;
using Editor.DearImGui;
using Editor.Demos;
using Editor.ExtConsole;
using Editor.UI.Windows;
using Editor.Interaction;
using Editor.Rendering;
using Editor.Storage;
using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Assets.Loaders;
using Editor.UI.Diagnostics;
using Editor.UI.Designer;
using Primary.Rendering;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Components;
using Primary.Mathematics;
using Primary.GUI.ImGui;
using Editor.Gui.Debugging;
using Primary.Profiling;
using Primary.Windowing;

namespace Editor.UI.Designer
{
    internal sealed class UIDesignerEngine : EditorRuntime
    {
        internal UIDesignerEngine(string baseProjectPath, string[] args) : base(baseProjectPath, args)
        {
        }

        public override void Run()
        {
            Window window = WindowManager.CreateWindow("UI designer", new Int2(1336, 726), CreateWindowFlags.Resizable);
            UIDockHost centralHost = UIManager.CreateHostedDock(window);

            RenderingManager.SetNewRenderPath(new EditorRenderPath());

            ImGuiManager.AddDrawer(new LayoutDebugger());
            ImGuiManager.AddDrawer(new DebugProfiler());

            DearImGuiStateManager.InitWindow(window);
            DearImGuiWindowManager.Open<UILayoutDebugger>();

            UIManager.OpenWindow<UIDesigner>(centralHost, "Editor/Designer/Base.layout");

            EventManager.AddHandler(ImGuiManager.Context.StateController);
            RenderingManager.RenderPassManager.AddRenderPass<ImGuiRenderPass>();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            while (!window.IsClosed)
            {
                Time.BeginNewFrame();
                ProfilingManager.StartProfilingForFrame();

                using (new ProfilingScope("Editor"))
                {
                    ExtConsoleManager?.PollUpdates();

                    _assetDatabase.HandlePendingUpdates();
                    _assetPipeline.PollRemainingEvents();
                    UIManager.UpdatePendingLayouts();
                    ToolManager.Update();

                    DrawDearImgui();
                }

                ThreadHelper.ExecutePendingTasks();

                InputSystem.UpdatePending();
                EventManager.PollEvents();
                SystemManager.RunSystems();
                ImGuiManager.UpdateAndRender();
                RenderingManager.Render();
            }
        }

        internal static UIDesignerEngine Instance => Unsafe.As<UIDesignerEngine>(GlobalSingleton);
    }
}
