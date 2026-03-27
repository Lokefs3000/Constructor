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
using Editor.UI.Debugging;
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

namespace Editor.UI.Designer
{
    internal sealed class UIDesignerEngine : Editor
    {
        internal UIDesignerEngine(string baseProjectPath, string[] args) : base(baseProjectPath, args)
        {
        }

        public override void Run()
        {
            Window window = WindowManager.CreateWindow("UI designer", new Int2(1336, 726), CreateWindowFlags.Resizable);
            UIDockHost centralHost = UIManager.CreateHostedDock(window);

            RenderingManager.SetNewRenderPath(new EditorRenderPath());

            DearImGuiStateManager.InitWindow(window);
            DearImGuiWindowManager.Open<UILayoutDebugger>();

            UIManager.OpenWindow<UIDesigner>(centralHost, "Editor/Designer/Base.layout");

            {
                Scene defaultScene = SceneManager.CreateScene("Default", LoadSceneMode.Single);

                SceneEntity cameraEntity = defaultScene.CreateEntity(SceneEntity.Null);
                cameraEntity.AddComponent<Camera>();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            while (!window.IsClosed)
            {
                Time.BeginNewFrame();
                ProfilingManager.StartProfilingForFrame();

                ExtConsoleManager?.PollUpdates();

                AssetDatabase.HandlePendingUpdates();
                AssetPipeline.PollRemainingEvents();

                ThreadHelper.ExecutePendingTasks();
                SystemManager.RunSystems();

                UIManager.UpdatePendingLayouts();

                DrawDearImgui();

                UIDebugRenderer.Draw(Gizmos.Instance, centralHost);

                InputSystem.UpdatePending();
                EventManager.PollEvents();

                RenderingManager.Render();
            }
        }

        internal static UIDesignerEngine Instance => Unsafe.As<UIDesignerEngine>(GlobalSingleton);
    }
}
