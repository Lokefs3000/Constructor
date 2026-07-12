using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using EditorUI;
using EditorUI.Demo;
using EditorUI.Diagnostics.ImGui;
using EditorUI.Dock;
using EditorUI.Serialization.Value;
using Primary;
using Primary.Assets;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Input;
using Primary.Mathematics;
using Primary.Windowing;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Inspector;
using PrimaryEditor.Project;
using PrimaryEditor.Reflection;
using PrimaryEditor.Rendering;
using PrimaryEditor.Startup;
using PrimaryEditor.UI;
using PrimaryEditor.UI.Serialization;
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
        private UIBridge _uiBridge;
        private EditorRenderingManager _editorRenderingManager;
        private AssemblyTypeLoader _typeLoader;
        private InspectorManager _inspectorManager;

        private bool _disposedValue;

        internal EditorRuntime(ReadOnlySpan<string> args) : base(args)
        {
            _uiManager = new UIManager();
            _uiBridge = new UIBridge(this);
            _editorRenderingManager = new EditorRenderingManager();
            _typeLoader = new AssemblyTypeLoader();
            _inspectorManager = new InspectorManager();

            _assetPipeline!.RegisterCustomAssets(_splash!);

            _typeLoader.AddCallback<ValueConverterAttribute>(_uiManager.ValueSerializer.LoadConverterFromType);
            _typeLoader.AddCallback<UIWidgetAttribute>(_uiManager.ReflectionManager.WidgetDatabase.LoadWidgetFromType);

            _typeLoader.ScanAssemblyForTypes(typeof(EditorRuntime).Assembly);
            _typeLoader.ScanAssemblyForTypes(typeof(UIManager).Assembly);
        }

        public override void Dispose()
        {
            _editorRenderingManager.Dispose();
            _uiManager.Dispose();

            _splash?.Dispose();

            base.Dispose();
        }

        internal void Run()
        {
            RenderingManager.SetNewRenderPath(new EditorRenderPath());

            ImGuiManager.AddDrawer(new HierchyExplorer());
            ImGuiManager.AddDrawer(new GuiStatistics());
            ImGuiManager.IsEnabled = true;

            EventManager.AddHandler(_uiManager.InputManager);

            _uiManager.ValueSerializer.LoadConverterFromType(typeof(FontFamilyValueConverter), default!);

            StylesheetAsset stylesheet = AssetManager.LoadAsset<StylesheetAsset>("Editor/UI/Stylesheets/Main.style").WaitIfNotLoaded();

            Display centerDisplay = WindowManager.PrimaryDisplay;

            WindowDock dock = _uiManager.DockManager.CreateWindowDock(DockFlags.SingleWindow);
            WindowDock bottomDock = _uiManager.DockManager.CreateWindowDock(DockFlags.None);
            WindowDock rightDock = _uiManager.DockManager.CreateWindowDock(DockFlags.None);

            // WindowDock floatingDock1 = _uiManager.DockManager.CreateWindowDock(DockFlags.SingleWindow | DockFlags.NoDocking);

            rightDock.TryDockInto(dock, DockingSide.Right);
            bottomDock.TryDockInto(dock, DockingSide.Bottom);

            rightDock.Space = 300;
            bottomDock.Space = 230;

            DockHost host = _uiManager.DockManager.CreateHostForDock(dock, true, new Rect(centerDisplay.FindCenter(new Int2(1336, 726)), new Int2(1336, 726)));
            // DockHost floatingHost1 = _uiManager.DockManager.CreateHostForDock(floatingDock1, false, new Rect(300, 300, 800, 500));
            
            host.StylesheetProvider.AddStylesheet(stylesheet.Stylesheet!);
            // floatingHost1.StylesheetProvider.AddStylesheet(stylesheet.Stylesheet!);

            _uiManager.WindowManager.OpenWidgetWindow<SceneViewWindow>()?.TryDockInto(dock);
            _uiManager.WindowManager.OpenWidgetWindow<ContentBrowserWindow>()?.TryDockInto(bottomDock);
            _uiManager.WindowManager.OpenWidgetWindow<GCProfilerWindow>()?.TryDockInto(bottomDock);
            _uiManager.WindowManager.OpenWidgetWindow<InspectorWindow>()?.TryDockInto(rightDock);

            // _uiManager.WindowManager.OpenWidgetWindow<LayoutInspectorWindow>()?.TryDockInto(floatingDock1);

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
                _editorRenderingManager.PrepareForFrame();
                _assetPipeline.HandleUpdates();
                _inspectorManager.UpdateContexts();
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
            if (AppArguments.HasArgument("suspend"))
                Console.ReadKey();

            _splash = new StartupSplash();

            _projectData = new ProjectData();
            _assetPipeline = new AssetPipeline(this);

            // initialize
            if (!AppArguments.TryGetValue("project-path", out string? projectPath))
            {
                Environment.Exit(1);
            }

            _projectData.SetupData(projectPath, _splash);
            _assetPipeline.ImportAnyChangesLaunch(_splash);
        }

        protected override void SetupFilesystems()
        {
            AssetFilesystem.AddFilesystem(_assetPipeline.FilesystemManager);
        }

        protected override void SetupAssets()
        {
            AssetManager.LockInIdProvider(_assetPipeline.AssetRegistry);
        }
        #endregion

        public ProjectData ProjectData => _projectData;
        public AssetPipeline AssetPipeline => _assetPipeline;

        public UIManager UIManager => _uiManager;
        public InspectorManager InspectorManager => _inspectorManager;

        public static EditorRuntime Instance => Unsafe.As<EditorRuntime>(Engine.GlobalSingleton);
    }
}
