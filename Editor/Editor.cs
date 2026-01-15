using Editor.Assets;
using Editor.Assets.Loaders;
using Editor.Assets.Types;
using Editor.Demos;
using Editor.UI;
using Editor.UI.Debugging;
using Editor.UI.Designer;
using Editor.Interaction;
using Editor.Platform.Windows;
using Editor.Project;
using Editor.Rendering;
using Editor.Storage;
using Hexa.NET.ImGui;
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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Editor.UI.Assets;

namespace Editor
{
    public class Editor : Engine
    {
        private readonly string _baseProjectPath;
        private readonly ProjectConfiguration _projectConfig;

        private DateTime _startTime;

        private ProjectSubFilesystem _projectSubFilesystem;

        private ProjectSubFilesystem _engineFilesystem;
        private ProjectSubFilesystem _editorFilesystem;

        private AssetDatabase _assetDatabase;
        private AssetPipeline _assetPipeline;

        //private DearImGuiStateManager _dearImGuiStateManager;
        //private DearImGuiWindowManager _dearImGuiWindowManager;
        //private EditorGuiManager _guiManager;
        private SelectionManager _selectionManager;
        private ToolManager _toolManager;
        private EditorRenderManager _editorRenderManager;

        private DynamicAtlasManager _guiAtlasManager;

        private UIManager _uiManager;

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

        internal Editor(string baseProjectPath, string[] args) : base(args.Length > 1 ? args.AsSpan(1) : Span<string>.Empty)
        {
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
            base.Initialize(_assetPipeline.Identifier);
            ui.PopStep();

            ui.PushStep("Initialize editor");

            RegisterComponentsDefault.RegisterDefault();

            //_dearImGuiStateManager = new DearImGuiStateManager(this);
            //_dearImGuiWindowManager = new DearImGuiWindowManager();
            //_guiManager = new EditorGuiManager();
            _selectionManager = new SelectionManager();
            _toolManager = new ToolManager();
            _editorRenderManager = new EditorRenderManager();

            _guiAtlasManager = new DynamicAtlasManager();

            _uiManager = new UIManager(EdLog.Gui);

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
        }

        public override void Dispose()
        {
            _guiAtlasManager.Dispose();

            _editorRenderManager.Dispose();
            //_dearImGuiStateManager.Dispose();
            _assetPipeline.Dispose();

            base.Dispose();
        }

        public void Run()
        {
            EdLog.Gui.Information("Hardware acceleration:");

            EdLog.Gui.Information("     Vector128: {b}", Vector128.IsHardwareAccelerated);
            EdLog.Gui.Information("         SSE: {b}", Sse.IsSupported);
            EdLog.Gui.Information("         SSE2: {b}", Sse2.IsSupported);
            EdLog.Gui.Information("         SSE3: {b}", Sse3.IsSupported);
            EdLog.Gui.Information("         SSE41: {b}", Sse41.IsSupported);
            EdLog.Gui.Information("         SSE42: {b}", Sse42.IsSupported);

            EdLog.Gui.Information("     Vector256: {b}", Vector256.IsHardwareAccelerated);
            EdLog.Gui.Information("         AVX: {b}", Avx.IsSupported);
            EdLog.Gui.Information("         AVX2: {b}", Avx2.IsSupported);

            EdLog.Gui.Information("     Vector512: {b}", Vector512.IsHardwareAccelerated);
            EdLog.Gui.Information("         AVX512: {b}", Avx512F.IsSupported);
            EdLog.Gui.Information("         AVX10.1: {b}", Avx10v1.IsSupported);

            Window window = WindowManager.CreateWindow("Primary", new Vector2(1336, 726), CreateWindowFlags.Resizable);
            UIDockHost centralHost = _uiManager.CreateHostedDock(window);

            _uiManager.OpenWindow<UIDesigner>(centralHost);

            RenderingManager.SetNewRenderPath(new EditorRenderPath());

            //_dearImGuiStateManager.InitWindow(window);

            //_dearImGuiWindowManager.Open<FrameGraphViewer>();
            //_dearImGuiWindowManager.Open<RenderPassInspector>();

            _guiAtlasManager.TriggerRebuild();

            AssetManager.RegisterCustomAsset<GeoSceneAsset>(new GeoSceneAssetLoader());

            SceneManager.CreateScene("Default", LoadSceneMode.Single);
            //Scene scene = SceneManager.CreateScene("Demo");

            try
            {
                StaticDemoScene3.Load(this);
            }
            catch (Exception ex) when (false)
            {
                EdLog.Core.Error(ex, "Demo scene load exception");
            }

            EventManager.PumpDefaultPause += PumpEditorLoop;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            _startTime = DateTime.Now;

            while (!window.IsClosed)
            {
                PumpEditorLoop();
            }
        }

        private void PumpEditorLoop()
        {
            Time.BeginNewFrame();
            ProfilingManager.StartProfilingForFrame();

            _editorRenderManager.PrepareFrame();

            _assetDatabase.HandlePendingUpdates();
            _assetPipeline.PollRemainingEvents();
            //_guiManager.Update();
            _uiManager.UpdatePendingLayouts();
            _toolManager.Update();

            ThreadHelper.ExecutePendingTasks();

            DrawDearImgui();

            UIDebugRenderer.Draw(Gizmos.Instance, _uiManager.ActiveHosts.First());

            InputSystem.UpdatePending();
            EventManager.PollEvents();
            SystemManager.RunSystems();
            RenderingManager.Render();
        }

        private void DrawDearImgui()
        {
            using (new ProfilingScope("EditorGui"))
            {
                //_dearImGuiStateManager.BeginFrame();

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

                //_dearImGuiStateManager.EndFrame();

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
        public DynamicAtlasManager GuiAtlasManager => _guiAtlasManager;
        public SelectionManager SelectionManager => _selectionManager;
        public ToolManager ToolManager => _toolManager;

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

        public static new Editor GlobalSingleton => Unsafe.As<Editor>(Engine.GlobalSingleton);
    }
}
