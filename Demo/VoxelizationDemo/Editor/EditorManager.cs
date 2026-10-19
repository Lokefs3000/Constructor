using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Profiling;
using Primary.Rendering.Diagnostics;
using Primary.Scenes;
using Primary.Timing;
using PrimaryEditor.Inspector;
using PrimaryEditor.Inspector.Contexts.Entity;
using PrimaryEditor.Search;
using PrimaryEditor.Selection;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Rendering;
using VoxelizationDemo.Editor.Rendering.Icons;
using VoxelizationDemo.Editor.Serialization;
using VoxelizationDemo.Editor.Tools;
using VoxelizationDemo.Editor.Windows;

namespace VoxelizationDemo.Editor
{
    internal sealed class EditorManager : IDisposable
    {
        private readonly SearchManager _searchManager;
        private readonly SelectionManager _selectionManager;

        private readonly ContextManager _contextManager;
        private readonly EditorWindowManager _windowManager;
        private readonly EditorRenderStage _renderStage;
        private readonly WorldIconManager _worldIconManager;
        private readonly InteractionGizmoManager _interactionGizmoManager;
        private readonly ViewportManager _viewportManager;
        private readonly EditorState _editorState;
        private readonly ShortcutsManager _shortcutsManager;
        private readonly SceneTracker _sceneTracker;
        private readonly HandleAllocator _handleAllocator;
        private readonly ToolManager _toolManager;
        private readonly PickingManager _pickingManager;
        private readonly SerializationManager _serializationManager;
        private readonly KeybindManager _keybindManager;

        private bool _isEnabled;

        private bool _disposedValue;

        internal EditorManager()
        {
            _searchManager = new SearchManager();
            _selectionManager = new SelectionManager();

            _contextManager = new ContextManager();
            _windowManager = new EditorWindowManager();
            _renderStage = new EditorRenderStage();
            _worldIconManager = new WorldIconManager();
            _interactionGizmoManager = new InteractionGizmoManager();
            _viewportManager = new ViewportManager();
            _editorState = new EditorState();
            _shortcutsManager = new ShortcutsManager();
            _sceneTracker = new SceneTracker();
            _handleAllocator = new HandleAllocator();
            _toolManager = new ToolManager();
            _pickingManager = new PickingManager();
            _serializationManager = new SerializationManager();
            _keybindManager = new KeybindManager();

            _isEnabled = false;

            _windowManager.AddOpenWindow(new StatisticsWindow());
            _windowManager.AddOpenWindow(new WorldAxisWindow());
            _windowManager.AddOpenWindow(new ToolsWindow());
            _windowManager.AddOpenWindow(new SceneWindow());

            InspectorManager inspectorManager = VoxelRuntime.Instance.InspectorManager;
            inspectorManager.OnInspectStart += (context) =>
            {
                if (!_windowManager.HasWindowOpen<InspectorWindow>())
                {
                    _windowManager.AddOpenWindow(new InspectorWindow(context));
                }
            };

            _selectionManager.OnObjectSelected += OnObjectSelectedCallback;
            _selectionManager.OnObjectDeselected += OnObjectDeselectedCallback;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_isEnabled)
                        Disable();

                    _selectionManager.Dispose();

                    _handleAllocator.Dispose();
                    _sceneTracker.Dispose();
                    _shortcutsManager.Dispose();
                    _windowManager.Dispose();
                    _contextManager.Dispose();
                    _keybindManager.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void OnObjectSelectedCallback(object obj)
        {
            InspectorManager inspectorManager = VoxelRuntime.Instance.InspectorManager;

            if (obj is SceneEntity entity)
            {
                inspectorManager.StartInspect<EntityInspectorContext, SceneEntity>(ref entity);
            }
        }

        private void OnObjectDeselectedCallback(object obj)
        {
            InspectorManager inspectorManager = VoxelRuntime.Instance.InspectorManager;

            if (obj is SceneEntity entity)
            {
                inspectorManager.StopInspect(ref entity);
            }
        }

        internal void Enable()
        {
            if (_isEnabled)
                return;

            VoxelRuntime runtime = VoxelRuntime.Instance;

            runtime.EventManager.AddHandler(_contextManager);
            runtime.RenderingManager.InstallRenderStage(_renderStage);

            runtime.RenderingManager.DebugManager.Enable();

            _isEnabled = true;
        }

        internal void Disable()
        {
            if (!_isEnabled)
                return;

            VoxelRuntime runtime = VoxelRuntime.Instance;

            runtime.EventManager.RemoveHandler(_contextManager);
            runtime.RenderingManager.UninstallRenderStage(_renderStage);

            _isEnabled = true;
        }

        internal void Update()
        {
            using (new ProfilingScope("Editor"))
            {
                _pickingManager.FlushFinishedPicks();
                _worldIconManager.CollectAllVisibleIcons();
                _interactionGizmoManager.UpdateGizmos();
                _handleAllocator.CleanupPrevious();
                _toolManager.UpdateTools();

                _contextManager.StartFrame();

                using (new ProfilingScope("Windows"))
                {
                    _windowManager.RenderActiveWindows();
                }

                // {
                //     ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
                //     ImGuiIOPtr io = ImGui.GetIO();
                // 
                //     drawList.AddText(new Vector2(20.0f, io.DisplaySize.Y * 0.5f), 0xffffffff, $"Frametime: {Time.DeltaTimeDouble * 1000.0:f4}ms ({(int)(1.0 / Time.DeltaTimeDouble)} fps)");
                //     drawList.AddText(new Vector2(20.0f, io.DisplaySize.Y * 0.5f + 11.0f), 0xffffffff, $"Position: {VoxelRuntime.Instance.CameraManager.Position:f4}");
                //     drawList.AddText(new Vector2(20.0f, io.DisplaySize.Y * 0.5f + 22.0f), 0xffffffff, $"GC Memory: {FileUtility.FormatSize(ProfilingManager.Instance.GCProfiler.CurrentMemoryUsage, "F2")} ({FileUtility.FormatSize(ProfilingManager.Instance.GCProfiler.AllocationRate, "F2")}/f)");
                // 
                // }

                Gizmos.DrawVector(Vector3.Zero);

                _viewportManager.UpdateViewport();
                _contextManager.EndFrame();
            }
        }

        public ContextManager ContextManager => _contextManager;
        public EditorWindowManager WindowManager => _windowManager;
        public WorldIconManager WorldIconManager => _worldIconManager;
        public InteractionGizmoManager InteractionGizmoManager => _interactionGizmoManager;
        public ViewportManager ViewportManager => _viewportManager;
        public EditorState EditorState => _editorState;
        public ShortcutsManager ShortcutsManager => _shortcutsManager;
        public SceneTracker SceneTracker => _sceneTracker;
        public HandleAllocator HandleAllocator => _handleAllocator;
        public ToolManager ToolManager => _toolManager;
        public PickingManager PickingManager => _pickingManager;
        public SerializationManager SerializationManager => _serializationManager;
        public KeybindManager KeybindManager => _keybindManager;

        public SearchManager SearchManager => _searchManager;
        public SelectionManager SelectionManager => _selectionManager;

        public static EditorManager Instance => VoxelRuntime.Instance.EditorManager;
    }
}
