using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EditorUI;
using EditorUI.Diagnostics.ImGui;
using EditorUI.Dock;
using Primary;
using Primary.Mathematics;
using PrimaryEditor.Assets;
using PrimaryEditor.Project;
using PrimaryEditor.Rendering;
using PrimaryEditor.Startup;
using PrimaryEditor.Windows;

namespace PrimaryEditor.Core
{
    public sealed class EditorRuntime : Engine
    {
        private StartupSplash? _splash;

        // before engine init
        private ProjectData _projectData;
        private AssetPipeline _assetPipeline;

        // after engine init
        private UIManager _uiManager;

        private bool _disposedValue;

        internal EditorRuntime(ReadOnlySpan<string> args) : base(args)
        {
            _uiManager = new UIManager();
        }

        public override void Dispose()
        {
            _uiManager.Dispose();

            _splash?.Dispose();

            base.Dispose();
        }

        internal void Run()
        {
            RenderingManager.SetNewRenderPath(new EditorRenderPath());

            ImGuiManager.AddDrawer(new HierchyExplorer());
            ImGuiManager.IsEnabled = true;

            WindowDock dock = _uiManager.DockManager.CreateWindowDock(DockFlags.SingleWindow);
            DockHost host = _uiManager.DockManager.CreateHostForDock(dock, true, new Rect(500, 400, 1336, 726));

            _uiManager.WindowManager.OpenWidgetWindow<EditorViewWindow>()?.TryDockInto(dock);

            {
                _splash?.Dispose();
                _splash = null;
            }

            while (true)
            {
                InternalEditorLoop();
            }
        }

        private void InternalEditorLoop()
        {
            Time.BeginNewFrame();
            ProfilingManager.StartProfilingForFrame();

            // editor only
            {
                _uiManager.UpdateInternalData();
            }

            ThreadHelper.ExecutePendingTasks();
            AssetManager.UpdateAssets();

            InputSystem.UpdatePending();
            EventManager.PollEvents();
            ImGuiManager.UpdateAndRender();
            RenderingManager.Render();
        }

        #region Implentation
        protected override void PreInitialization()
        {
            _splash = new StartupSplash();

            _projectData = new ProjectData();
            _assetPipeline = new AssetPipeline();

            // initialize
            _projectData.SetupData(string.Empty, _splash);
            _assetPipeline.ImportAnyChangesLaunch(_splash);
        }
        #endregion

        public AssetPipeline AssetPipeline => _assetPipeline;

        public UIManager UIManager => _uiManager;

        public static EditorRuntime Instance => Unsafe.As<EditorRuntime>(Engine.GlobalSingleton);
    }
}
