using Editor.Assets;
using Editor.Assets.Loaders;
using Editor.Assets.Types;
using Editor.DearImGui;
using Editor.ExtConsole;
using Editor.Interaction;
using Editor.Platform.Windows;
using Editor.Project;
using Editor.Rendering;
using Editor.Storage;
using Editor.UI;
using Primary;
using Primary.Assets;
using Primary.Components;
using Primary.Polling;
using Primary.Profiling;
using Primary.R2.ForwardPlus;
using Primary.Rendering;
using Primary.Scenes;
using Primary.Timing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Primary.Mathematics;
using Primary.GUI.ImGui;
using Editor.Rendering.Passes;
using Editor.Gui.Windows;
using Editor.Reflection;
using Editor.Geo.Passes;
using Editor.Rendering.Tools;
using Editor.Geo;
using Primary.Windowing;
using Editor.UI.Diagnostics;
using Editor.Processors.Texture;
using Editor.History;
using Editor.IO;
using Editor.Inspector;

namespace Editor
{
    public class EditorRuntime : Engine
    {
        private readonly string _baseProjectPath;
        private readonly ProjectConfiguration _projectConfig;

        private DateTime _startTime;

        protected ProjectSubFilesystem _projectSubFilesystem;

        protected ProjectSubFilesystem _engineFilesystem;
        protected ProjectSubFilesystem _editorFilesystem;

        protected AssetDatabase _assetDatabase;
        protected AssetPipeline _assetPipeline;

        private DearImGuiStateManager _dearImGuiStateManager;
        private DearImGuiWindowManager _dearImGuiWindowManager;
        //private EditorGuiManager _guiManager;
        private EditorRenderManager _editorRenderManager;

        private DynamicAtlasManager _guiAtlasManager;

        private UIManager _uiManager;
        private GeoSceneManager _geoSceneManager;
        private ReflectionManager _reflectionManager;

        private EditorView _editorView;
        private SelectionManager _selectionManager;
        private ToolManager _toolManager;
        private HistoryManager _historyManager;
        private EditorClipboard _editorClipboard;
        private InspectorManager _inspectorManager;

        private ExtConsoleManager? _extConsoleManager;

        //private ProfilerView2 _profilerView2;
        //private HierchyView _hierchyView;
        //private PropertiesView _propertiesView;
        //private RenderingView _renderingView;
        //private EditorTaskViewer _editorTaskViewer;
        //private ContentView _contentView;
        //private SceneView _sceneView;
        //private ImportFileView _importFileView;
        //private PopupManager _popupManager;
        //private InputDebugger _inputDebugger;
        //private DebugView _debugView;
        //private GeoEditorView _geoEditorView;
        //private BundleExplorer _bundleExplorer;
        //private ConsoleView _consoleView;
        //private CubemapTool _cubemapTool;

        internal EditorRuntime(string baseProjectPath, string[] args, bool dontLaunch = false) : base(args.Length > 1 ? args.AsSpan(1) : Span<string>.Empty)
        {
            if (dontLaunch)
            {
                if (!baseProjectPath.EndsWith(Path.DirectorySeparatorChar))
                    baseProjectPath += Path.DirectorySeparatorChar;

                VerifyProjectPath(baseProjectPath);

                _baseProjectPath = baseProjectPath;

                EditorFilepaths.Initialize(baseProjectPath);
                return;
            }

            long startupTimestamp = Stopwatch.GetTimestamp();
            using StartupDisplayUI ui = new StartupDisplayUI();

            CancellationTokenSource cts = new CancellationTokenSource();
            Task pumpTask = Task.Factory.StartNew(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    ui.Poll();
                    ui.Draw();
                }
            });

            if (AppArguments.HasArgument("--inf-splash"))
            {
                ui.PushStep("Hello, world!", "Hi, im a description!");
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        ui.Progress = (ui.Progress + 0.01f) % 1.0f;
                        Thread.Sleep(10);
                    }
                });

                Console.ReadLine();
                Environment.Exit(0);
            }

            if (!baseProjectPath.EndsWith(Path.DirectorySeparatorChar))
                baseProjectPath += Path.DirectorySeparatorChar;

            VerifyProjectPath(baseProjectPath);

            _baseProjectPath = baseProjectPath;
            _projectConfig = new ProjectConfiguration(Path.Combine(_baseProjectPath, "Project.toml"));

            EditorFilepaths.Initialize(baseProjectPath);

            ui.PushStep("Setup filesystem");

            _projectSubFilesystem = new ProjectSubFilesystem(EditorFilepaths.ContentPath);

            _engineFilesystem = new ProjectSubFilesystem(@"D:/source/repos/Constructor/Source/Engine");
            _editorFilesystem = new ProjectSubFilesystem(@"D:/source/repos/Constructor/Source/Editor");

            _assetDatabase = new AssetDatabase();
            _assetPipeline = new AssetPipeline(ui);

            ui.PopStep();

            ui.PushStep("Initialize engine");
            base.Initialize(_assetPipeline.Identifier, _assetPipeline.Cache);
            ui.PopStep();

            ui.PushStep("Initialize editor");

            RegisterComponentsDefault.RegisterDefault();

            _dearImGuiStateManager = new DearImGuiStateManager(this);
            _dearImGuiWindowManager = new DearImGuiWindowManager();
            //_guiManager = new EditorGuiManager();
            _editorRenderManager = new EditorRenderManager();

            _guiAtlasManager = new DynamicAtlasManager();

            _uiManager = new UIManager(EdLog.Gui);
            _geoSceneManager = new GeoSceneManager();
            _reflectionManager = new ReflectionManager();

            _editorView = new EditorView();
            _selectionManager = new SelectionManager();
            _toolManager = new ToolManager(this);
            _historyManager = new HistoryManager();
            _editorClipboard = new EditorClipboard();
            _inspectorManager = new InspectorManager();

            if (AppArguments.HasArgument("--with-extcon"))
                _extConsoleManager = new ExtConsoleManager(ui);

            //_profilerView2 = new ProfilerView2(_guiAtlasManager);
            //_hierchyView = new HierchyView();
            //_propertiesView = new PropertiesView();
            //_renderingView = new RenderingView();
            //_editorTaskViewer = new EditorTaskViewer();
            //_contentView = new ContentView();
            //_sceneView = new SceneView(_guiAtlasManager);
            //_importFileView = new ImportFileView();
            //_popupManager = new PopupManager();
            //_inputDebugger = new InputDebugger();
            //_debugView = new DebugView();
            //_geoEditorView = new GeoEditorView();
            //_bundleExplorer = new BundleExplorer();
            //_consoleView = new ConsoleView();
            //_cubemapTool = new CubemapTool();

            ui.PopStep();

            cts.Cancel();
            pumpTask.Wait();
            cts.Dispose();

            EdLog.Core.Information("Editor startup took: {secs:f4}s", Stopwatch.GetElapsedTime(startupTimestamp).TotalSeconds);
        }

        public override void Dispose()
        {
            _extConsoleManager?.Dispose();

            _historyManager.Dispose();

            _guiAtlasManager.Dispose();

            _uiManager.Dispose();
            _dearImGuiStateManager.Dispose();
            _editorRenderManager.Dispose();
            _dearImGuiStateManager.Dispose();
            _assetPipeline.Dispose();

            base.Dispose();
        }

        public virtual void Run()
        {
            Window window = WindowManager.CreateWindow("Primary", new Int2(1336, 726), CreateWindowFlags.Resizable);
            WindowManager.PrimaryWindow = window;

            UIDockHost centralHost = _uiManager.CreateHostedDock(window);
            UIDockHost leftHost = _uiManager.CreateDockedHost(centralHost, UIDockSide.Left);
            UIDockHost rightHost = _uiManager.CreateDockedHost(centralHost, UIDockSide.Right);

            //UIDockHost floatingHost = _uiManager.CreateFloatingDock(new Int2(700, 500));

            leftHost.SetHostSize(250);
            rightHost.SetHostSize(250);

            //UIDockHost bottomHost = _uiManager.CreateDockedHost(centralHost, UIDockSide.Bottom);
            //bottomHost.SetHostSize(window.ClientSize.Y * 0.5f);

            //_uiManager.OpenWindow<UIDesigner>(centralHost);
            _uiManager.OpenWindow<EditorViewWindow>(centralHost, "Editor/UI/EditorView.layout");
            _uiManager.OpenWindow<HierchyWindow>(leftHost, "Editor/UI/Hierchy.layout");
            _uiManager.OpenWindow<InspectorWindow>(rightHost, "Editor/UI/Inspector.layout");

            //_uiManager.OpenWindow<DebugWindow>(floatingHost, "Editor/UI/Diagnostics/DebugWindow.layout");
            
            RenderingManager.SetNewRenderPath(new ForwardPlusRenderPath());

            _dearImGuiStateManager.InitWindow(window);

            //_dearImGuiWindowManager.Open<FrameGraphViewer>();
            //_dearImGuiWindowManager.Open<RenderPassInspector>();
            _dearImGuiWindowManager.Open<UILayoutDebugger>();

            _guiAtlasManager.TriggerRebuild();

            AssetManager.RegisterCustomAsset<GeoSceneAsset>(new GeoSceneAssetLoader());
   
            SceneManager.LoadScene("Content/infra_import/Scenes/MapScene.scene", LoadSceneMode.Single);
            //SceneManager.LoadScene("Content/infra_import/Scenes/MapPlanesScene.scene", LoadSceneMode.Additive);
            //Scene scene = SceneManager.CreateScene("Demo");

            //try
            //{
            //    StaticDemoScene3.Load(this);
            //}
            //catch (Exception ex) when (false)
            //{
            //    EdLog.Core.Error(ex, "Demo scene load exception");
            //}

            EventManager.PumpDefaultPause += PumpEditorLoop;

            EventManager.AddHandler(ImGuiManager.Context.StateController);

            UIManager.Renderer.InstallRenderPasses(RenderingManager.RenderPassManager);
            RenderingManager.RenderPassManager.AddRenderPass<DearImGuiRenderPass>();
            RenderingManager.RenderPassManager.AddRenderPass<GeoScenePass>();
            RenderingManager.RenderPassManager.AddRenderPass<GizmoRenderPass>();
            RenderingManager.RenderPassManager.AddRenderPass<BrushSelectionPass>();
            RenderingManager.RenderPassManager.AddRenderPass<ToolsRenderPass>();

            _reflectionManager.ReflectCurrentData();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _startTime = DateTime.Now;

            while (!window.IsClosed)
            {
                PumpEditorLoop();
            }
        }

        protected virtual void PumpEditorLoop()
        {
            Time.BeginNewFrame();
            ProfilingManager.StartProfilingForFrame();

            using (new ProfilingScope("Editor"))
            {
                _extConsoleManager?.PollUpdates();

                _editorRenderManager.PrepareFrame();

                _assetDatabase.HandlePendingUpdates();
                _assetPipeline.PollRemainingEvents();
                _uiManager.UpdatePendingLayouts();
                _geoSceneManager.UpdateData();
                _toolManager.Update();

                _editorView.UpdateState();

                DrawDearImgui();
            }

            ThreadHelper.ExecutePendingTasks();
            AssetManager.UpdateAssets();
            SceneManager.UpdateScenes();
            ScriptingManager.ProcessPendingScripts();

            InputSystem.UpdatePending();
            EventManager.PollEvents();
            SystemManager.RunSystems();
            ImGuiManager.UpdateAndRender();
            RenderingManager.Render();
        }

        protected void DrawDearImgui()
        {
            using (new ProfilingScope("DearImGui"))
            {
                _dearImGuiStateManager.BeginFrame();

                /*if (ImGui.Begin("Profiler"))
                {
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                    ImGui.Text($"Frametime: {Time.DeltaTimeDouble.ToString("F5")} ({(1.0 / Time.DeltaTimeDouble).ToString("F3")})");

                    Dictionary<int, ThreadProfilingTimestamps> threadTimestamps = ProfilingManager.Timestamps;
                    foreach (var kvp in threadTimestamps)
                    {
                        DrawProfilerViewFor(drawList, kvp.Value, ProfilingManager.StartTimestamp);
                    }
                }
                ImGui.End();*/

                //if (ImGui.BeginMainMenuBar())
                //{
                //    _debugView.MenuBar();
                //
                //    if (ImGui.BeginMenu("View"))
                //    {
                //        _bundleExplorer.MenuBar();
                //        ImGui.EndMenu();
                //    }
                //
                //    ImGui.EndMainMenuBar();
                //}

                //_profilerView2.Render();
                //_hierchyView.Render();
                //_propertiesView.Render();
                //_renderingView.Render();
                //_editorTaskViewer.Render();
                //_contentView.Render();
                //_sceneView.Render();
                //_importFileView.Render();
                //_popupManager.Render();
                //_inputDebugger.Render();
                //_debugView.Render();
                //_geoEditorView.Render();
                //_bundleExplorer.Render();
                //_consoleView.Render();
                //_cubemapTool.Render();
                //_dearImGuiWindowManager.RenderOpenWindows();

                //ImGui.Text($"Runtime: {(DateTime.Now - _startTime).TotalSeconds}s");

                //GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
                //
                //ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
                //drawList.AddText(new Vector2(20.0f), 0xffffffff, $"Frametime: {Time.DeltaTimeDouble.ToString("F5", CultureInfo.InvariantCulture)} ({(1.0 / Time.DeltaTimeDouble).ToString("F1", CultureInfo.InvariantCulture)} fps)");
                //drawList.AddText(new Vector2(20.0f, 32.0f), 0xffffffff, $"Working set: {(_process.WorkingSet64 / (1024.0 * 1024.0)).ToString("F6", CultureInfo.InvariantCulture)}mb");
                //drawList.AddText(new Vector2(20.0f, 44.0f), 0xffffffff, $"Jit: il:{(JitInfo.GetCompiledILBytes() / 1024.0).ToString("F2", CultureInfo.InvariantCulture)}kb  mc:{JitInfo.GetCompiledMethodCount()}  ct:{JitInfo.GetCompilationTime()}");
                //drawList.AddText(new Vector2(20.0f, 56.0f), 0xffffffff, $"GC: {(GC.GetTotalMemory(false) / (1024.0 * 1024.0)).ToString("F6")}mb");

                _dearImGuiWindowManager.RenderOpenWindows();

                _dearImGuiStateManager.EndFrame();

                _timer += Time.DeltaTime;
                if (_timer > 2.0f)
                {
                    _process.Refresh();
                    _timer = 0.0f;
                }

                //_timer2 += Time.DeltaTime;
                //if (_timer2 > 10.0f)
                //{
                //    EdLog.Core.Information("Runtime: {secs}s", (DateTime.Now - _startTime).TotalSeconds);
                //    _timer2 = 0.0f;
                //}
            }
        }

        private readonly Process _process = Process.GetCurrentProcess();
        private float _timer = 0.0f;
        private float _timer2 = 0.0f;

        protected override void SetupAssetFilesystem()
        {
            AssetFilesystem.AddFilesystem(_projectSubFilesystem);
            AssetFilesystem.AddFilesystem(_engineFilesystem);
            AssetFilesystem.AddFilesystem(_editorFilesystem);

            _assetPipeline.PollRemainingEvents();
        }

        public string ProjectPath => _baseProjectPath;
        public ProjectConfiguration ProjectConfig => _projectConfig;

        internal ProjectSubFilesystem ProjectSubFilesystem => _projectSubFilesystem;

        internal ProjectSubFilesystem EngineFilesystem => _engineFilesystem;
        internal ProjectSubFilesystem EditorFilesystem => _editorFilesystem;

        public AssetDatabase AssetDatabase => _assetDatabase;
        public AssetPipeline AssetPipeline => _assetPipeline;

        public EditorRenderManager EditorRenderManager => _editorRenderManager;
        public DynamicAtlasManager GuiAtlasManager => _guiAtlasManager;

        public UIManager UIManager => _uiManager;
        public GeoSceneManager GeoSceneManager => _geoSceneManager;
        public ReflectionManager ReflectionManager => _reflectionManager;

        public EditorView EditorView => _editorView;
        public SelectionManager SelectionManager => _selectionManager;
        public ToolManager ToolManager => _toolManager;
        public HistoryManager HistoryManager => _historyManager;
        public EditorClipboard EditorClipboard => _editorClipboard;
        public InspectorManager InspectorManager => _inspectorManager;

        internal DearImGuiStateManager DearImGuiStateManager => _dearImGuiStateManager;
        public DearImGuiWindowManager DearImGuiWindowManager => _dearImGuiWindowManager;

        internal ExtConsoleManager? ExtConsoleManager => _extConsoleManager;

        //internal PropertiesView PropertiesView => _propertiesView;
        //internal SceneView SceneView => _sceneView;
        //internal PopupManager PopupManager => _popupManager;
        //internal GeoEditorView GeoEditorView => _geoEditorView;

        private static void VerifyProjectPath(string path)
        {
            if (!(Directory.Exists(Path.Combine(path, "Content")) && Directory.Exists(Path.Combine(path, "Source")) && Directory.Exists(Path.Combine(path, "Library")) && File.Exists(Path.Combine(path, "Project.toml"))))
            {
                throw new ArgumentException("Invalid project path!");
            }
        }

        public static new EditorRuntime GlobalSingleton => Unsafe.As<EditorRuntime>(Engine.GlobalSingleton);
    }
}
