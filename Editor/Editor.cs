using Editor.Assets;
using Editor.Assets.Loaders;
using Editor.Assets.Types;
using Editor.DearImGui;
using Editor.Gui;
using Editor.Interaction;
using Editor.Rendering;
using Editor.Storage;
using Hexa.NET.ImGui;
using Primary;
using Primary.Assets;
using Primary.Components;
using Primary.Mathematics;
using Primary.Polling;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Scenes;
using Primary.Timing;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor
{
    public class Editor : Engine
    {
        private readonly string _baseProjectPath;

        private ProjectSubFilesystem _projectSubFilesystem;
        private ProjectShaderLibrary _projectShaderLibrary;

        private ProjectSubFilesystem _engineFilesystem;
        private ProjectSubFilesystem _editorFilesystem;

        private AssetDatabase _assetDatabase;
        private AssetPipeline _assetPipeline;
        private DearImGuiStateManager _dearImGuiStateManager;
        //private EditorGuiManager _guiManager;
        private SelectionManager _selectionManager;
        private ToolManager _toolManager;
        private EditorRenderManager _editorRenderManager;

        private DynamicAtlasManager _guiAtlasManager;

        private ProfilerView _profilerView;
        private HierchyView _hierchyView;
        private PropertiesView _propertiesView;
        private RenderingView _renderingView;
        private EditorTaskViewer _editorTaskViewer;
        private ContentView _contentView;
        private SceneView _sceneView;
        private ImportFileView _importFileView;
        private RHIInspector _rhiInsector;
        private PopupManager _popupManager;
        private InputDebugger _inputDebugger;
        private DebugView _debugView;
        private GeoEditorView _geoEditorView;

        internal Editor(string baseProjectPath) : base()
        {
            if (!baseProjectPath.EndsWith(Path.DirectorySeparatorChar))
                baseProjectPath += Path.DirectorySeparatorChar;

            VerifyProjectPath(baseProjectPath);

            _baseProjectPath = baseProjectPath;
            EditorFilepaths.Initialize(baseProjectPath);

            _projectSubFilesystem = new ProjectSubFilesystem(EditorFilepaths.ContentPath);
            _projectShaderLibrary = new ProjectShaderLibrary(EditorFilepaths.ContentPath);

            _engineFilesystem = new ProjectSubFilesystem(@"D:/source/repos/Constructor/Source/Engine");
            _editorFilesystem = new ProjectSubFilesystem(@"D:/source/repos/Constructor/Source/Editor");

            _assetDatabase = new AssetDatabase();
            _assetPipeline = new AssetPipeline();

            base.Initialize(_assetPipeline.Identifier);

            RegisterComponentsDefault.RegisterDefault();

            _dearImGuiStateManager = new DearImGuiStateManager(this);
            //_guiManager = new EditorGuiManager();
            _selectionManager = new SelectionManager();
            _toolManager = new ToolManager();
            _editorRenderManager = new EditorRenderManager();

            _guiAtlasManager = new DynamicAtlasManager();

            _profilerView = new ProfilerView();
            _hierchyView = new HierchyView();
            _propertiesView = new PropertiesView();
            _renderingView = new RenderingView();
            _editorTaskViewer = new EditorTaskViewer();
            _contentView = new ContentView();
            _sceneView = new SceneView(_guiAtlasManager);
            _importFileView = new ImportFileView();
            _rhiInsector = new RHIInspector();
            _popupManager = new PopupManager();
            _inputDebugger = new InputDebugger();
            _debugView = new DebugView();
            _geoEditorView = new GeoEditorView();
        }

        public override void Dispose()
        {
            _profilerView.Dispose();

            _guiAtlasManager.Dispose();

            _editorRenderManager.Dispose();
            _dearImGuiStateManager.Dispose();
            _assetPipeline.Dispose();

            base.Dispose();
        }

        public void Run()
        {
            Window window = WindowManager.CreateWindow("Primary", new Vector2(1336, 726), CreateWindowFlags.Resizable);
            RenderingManager.DefaultWindow = window;

            RenderingManager.PostRender += _editorRenderManager.SetupPasses;

            _dearImGuiStateManager.InitWindow(window);
            _guiAtlasManager.TriggerRebuild();

            AssetManager.RegisterCustomAsset<GeoSceneAsset>(new GeoSceneAssetLoader());

            SceneManager.CreateScene("Default", LoadSceneMode.Single);
            //Scene scene = SceneManager.CreateScene("Demo");

            //StaticDemoScene2.Load(this);

            EventManager.PumpDefaultPause += PumpEditorLoop;

            GC.Collect();
            GC.WaitForPendingFinalizers();

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
            _toolManager.Update();

            ThreadHelper.ExecutePendingTasks();

            DrawDearImgui();

            InputSystem.UpdatePending();
            EventManager.PollEvents();
            SystemManager.RunSystems();
            RenderingManager.ExecuteRender();
        }

        private void DrawDearImgui()
        {
            using (new ProfilingScope("EditorGui"))
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

                if (ImGui.BeginMainMenuBar())
                {
                    _debugView.MenuBar();

                    ImGui.EndMainMenuBar();
                }

                _profilerView.Render();
                _hierchyView.Render();
                _propertiesView.Render();
                //_renderingView.Render();
                _editorTaskViewer.Render();
                _contentView.Render();
                _sceneView.Render();
                _importFileView.Render();
                _rhiInsector.Render();
                _popupManager.Render();
                _inputDebugger.Render();
                _debugView.Render();
                _geoEditorView.Render();

                //GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
                //
                //ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
                //drawList.AddText(new Vector2(20.0f), 0xffffffff, $"Frametime: {Time.DeltaTimeDouble.ToString("F5", CultureInfo.InvariantCulture)} ({(1.0 / Time.DeltaTimeDouble).ToString("F1", CultureInfo.InvariantCulture)} fps)");
                //drawList.AddText(new Vector2(20.0f, 32.0f), 0xffffffff, $"Working set: {(_process.WorkingSet64 / (1024.0 * 1024.0)).ToString("F6", CultureInfo.InvariantCulture)}mb");
                //drawList.AddText(new Vector2(20.0f, 44.0f), 0xffffffff, $"Jit: il:{(JitInfo.GetCompiledILBytes() / 1024.0).ToString("F2", CultureInfo.InvariantCulture)}kb  mc:{JitInfo.GetCompiledMethodCount()}  ct:{JitInfo.GetCompilationTime()}");
                //drawList.AddText(new Vector2(20.0f, 56.0f), 0xffffffff, $"GC: {(GC.GetTotalMemory(false) / (1024.0 * 1024.0)).ToString("F6")}mb");

                _dearImGuiStateManager.EndFrame();

                _timer += Time.DeltaTime;
                if (_timer > 2.0f)
                {
                    _process.Refresh();
                    _timer = 0.0f;
                }
            }
        }

        private readonly Process _process = Process.GetCurrentProcess();
        private float _timer = 0.0f;

        protected override void SetupAssetFilesystem()
        {
            AssetFilesystem.AddFilesystem(_projectSubFilesystem);
            AssetFilesystem.AddFilesystem(_engineFilesystem);
            AssetFilesystem.AddFilesystem(_editorFilesystem);

            AssetFilesystem.ShaderLibrary.AddSubLibrary(_projectShaderLibrary);

            _assetPipeline.PollRemainingEvents();
        }

        public string ProjectPath => _baseProjectPath;

        internal ProjectSubFilesystem ProjectSubFilesystem => _projectSubFilesystem;
        internal ProjectShaderLibrary ProjectShaderLibrary => _projectShaderLibrary;

        internal ProjectSubFilesystem EngineFilesystem => _engineFilesystem;
        internal ProjectSubFilesystem EditorFilesystem => _editorFilesystem;

        internal DearImGuiStateManager DearImGuiStateManager => _dearImGuiStateManager;
        public AssetDatabase AssetDatabase => _assetDatabase;
        public AssetPipeline AssetPipeline => _assetPipeline;
        public DynamicAtlasManager GuiAtlasManager => _guiAtlasManager;
        public SelectionManager SelectionManager => _selectionManager;
        public ToolManager ToolManager => _toolManager;

        internal PropertiesView PropertiesView => _propertiesView;
        internal SceneView SceneView => _sceneView;
        internal PopupManager PopupManager => _popupManager;
        internal GeoEditorView GeoEditorView => _geoEditorView;

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
