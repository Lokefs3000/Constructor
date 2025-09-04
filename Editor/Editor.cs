using Arch.Core;
using Editor.Assets;
using Editor.DearImGui;
using Editor.Demos;
using Editor.Gui;
using Editor.Rendering.Gizmos;
using Hexa.NET.ImGui;
using Primary;
using Primary.Assets;
using Primary.Common;
using Primary.Components;
using Primary.Polling;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Scenes;
using Primary.Timing;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Editor
{
    public class Editor : Engine
    {
        private readonly string _baseProjectPath;

        private ProjectSubFilesystem _projectSubFilesystem;
        private ProjectShaderLibrary _projectShaderLibrary;

        private AssetPipeline _assetPipeline;
        private DearImGuiStateManager _dearImGuiStateManager;
        //private EditorGuiManager _guiManager;

        private DynamicAtlasManager _guiAtlasManager;

        private ProfilerView _profilerView;
        private HierchyView _hierchyView;
        private PropertiesView _propertiesView;
        private RenderingView _renderingView;
        private EditorTaskViewer _editorTaskViewer;
        private ContentView _contentView;
        private SceneView _sceneView;

        internal Editor(string baseProjectPath) : base()
        {
            if (!baseProjectPath.EndsWith(Path.DirectorySeparatorChar))
                baseProjectPath += Path.DirectorySeparatorChar;

            VerifyProjectPath(baseProjectPath);

            _baseProjectPath = baseProjectPath;
            EditorFilepaths.Initialize(baseProjectPath);

            _projectSubFilesystem = new ProjectSubFilesystem(EditorFilepaths.ContentPath);
            _projectShaderLibrary = new ProjectShaderLibrary(EditorFilepaths.ContentPath);

            _assetPipeline = new AssetPipeline();

            base.Initialize();

            _dearImGuiStateManager = new DearImGuiStateManager(this);
            //_guiManager = new EditorGuiManager();

            _guiAtlasManager = new DynamicAtlasManager();

            _profilerView = new ProfilerView();
            _hierchyView = new HierchyView();
            _propertiesView = new PropertiesView();
            _renderingView = new RenderingView();
            _editorTaskViewer = new EditorTaskViewer();
            _contentView = new ContentView();
            _sceneView = new SceneView();
        }

        public override void Dispose()
        {
            _profilerView.Dispose();

            _guiAtlasManager.Dispose();

            _dearImGuiStateManager.Dispose();
            _assetPipeline.Dispose();

            base.Dispose();
        }

        public void Run()
        {
            Window window = WindowManager.CreateWindow("Primary", new Vector2(1336, 726), CreateWindowFlags.None);
            RenderingManager.DefaultWindow = window;

            RenderingManager.RenderPassManager.AddRenderPass<GizmoRenderPass>();

            _dearImGuiStateManager.InitWindow(window);
            _guiAtlasManager.TriggerRebuild();

            Scene scene = SceneManager.CreateScene("Demo");

            StaticDemoScene2.Load(this);

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
            DrawDearImgui();

            Time.BeginNewFrame();
            ProfilingManager.StartProfilingForFrame();

            _assetPipeline.PollRemainingEvents();
            //_guiManager.Update();

            ThreadHelper.ExecutePendingTasks();

            EventManager.PollEvents();
            SystemManager.RunSystems();
            RenderingManager.ExecuteRender();
        }

        private static Vector3 _cameraPosition;
        private static Vector2 _cameraRotation;

        private void DrawDearImgui()
        {
            _dearImGuiStateManager.BeginFrame();

            if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                Vector2 drag = ImGui.GetIO().MouseDelta;
                drag *= -0.2f;
                _cameraRotation += drag;
            }

            Quaternion quat = Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(_cameraRotation.X), float.DegreesToRadians(-_cameraRotation.Y), 0.0f);

            if (ImGui.IsKeyDown(ImGuiKey.W))
                _cameraPosition += Vector3.Transform(Vector3.UnitZ, quat) * 10.0f * Time.DeltaTime;
            if (ImGui.IsKeyDown(ImGuiKey.S))
                _cameraPosition += Vector3.Transform(-Vector3.UnitZ, quat) * 10.0f * Time.DeltaTime;
            if (ImGui.IsKeyDown(ImGuiKey.A))
                _cameraPosition += Vector3.Transform(Vector3.UnitX, quat) * 10.0f * Time.DeltaTime;
            if (ImGui.IsKeyDown(ImGuiKey.D))
                _cameraPosition += Vector3.Transform(-Vector3.UnitX, quat) * 10.0f * Time.DeltaTime;
            if (ImGui.IsKeyDown(ImGuiKey.E))
                _cameraPosition += Vector3.Transform(Vector3.UnitY, quat) * 10.0f * Time.DeltaTime;
            if (ImGui.IsKeyDown(ImGuiKey.Q))
                _cameraPosition += Vector3.Transform(-Vector3.UnitY, quat) * 10.0f * Time.DeltaTime;

            SceneManager.World.Query(new QueryDescription().WithAll<Camera, Transform>(), (ref Camera c, ref Transform t) =>
                {
                    t.Rotation = quat;
                    t.Position = _cameraPosition;
                });

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

            _profilerView.Render();
            _hierchyView.Render();
            _propertiesView.Render();
            _renderingView.Render();
            _editorTaskViewer.Render();
            _contentView.Render();
            _sceneView.Render();
            //_debugStatisticsView.Render();

            GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();

            ImDrawListPtr drawList = ImGui.GetForegroundDrawList();
            drawList.AddText(new Vector2(20.0f), 0xffffffff, $"Frametime: {Time.DeltaTimeDouble.ToString("F5", CultureInfo.InvariantCulture)} ({(1.0 / Time.DeltaTimeDouble).ToString("F1", CultureInfo.InvariantCulture)} fps)");
            drawList.AddText(new Vector2(20.0f, 32.0f), 0xffffffff, $"Working set: {(_process.WorkingSet64 / (1024.0 * 1024.0)).ToString("F6", CultureInfo.InvariantCulture)}mb");
            drawList.AddText(new Vector2(20.0f, 44.0f), 0xffffffff, $"Jit: il:{(JitInfo.GetCompiledILBytes() / 1024.0).ToString("F2", CultureInfo.InvariantCulture)}kb  mc:{JitInfo.GetCompiledMethodCount()}  ct:{JitInfo.GetCompilationTime()}");
            drawList.AddText(new Vector2(20.0f, 56.0f), 0xffffffff, $"GC: {(GC.GetTotalMemory(false) / (1024.0 * 1024.0)).ToString("F6")}mb");

            _dearImGuiStateManager.EndFrame();

            _timer += Time.DeltaTime;
            if (_timer > 2.0f)
            {
                _process.Refresh();
                _timer = 0.0f;
            }
        }

        private readonly Process _process = Process.GetCurrentProcess();
        private float _timer = 0.0f;

        protected override void SetupAssetFilesystem()
        {
            AssetFilesystem.AddFilesystem(_projectSubFilesystem);
            AssetFilesystem.ShaderLibrary.AddSubLibrary(_projectShaderLibrary);

            _assetPipeline.PollRemainingEvents();
        }

        public string ProjectPath => _baseProjectPath;

        internal ProjectSubFilesystem ProjectSubFilesystem => _projectSubFilesystem;
        internal ProjectShaderLibrary ProjectShaderLibrary => _projectShaderLibrary;

        public AssetPipeline AssetPipeline => _assetPipeline;
        public DynamicAtlasManager GuiAtlasManager => _guiAtlasManager;

        internal PropertiesView PropertiesView => _propertiesView;

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
