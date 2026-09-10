using System;
using System.Collections.Generic;
using System.Text;
using Primary.Profiling;
using Primary.Scenes;
using PrimaryEditor.Inspector;
using PrimaryEditor.Inspector.Contexts.Entity;
using PrimaryEditor.Search;
using PrimaryEditor.Selection;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Rendering;
using VoxelizationDemo.Editor.Rendering.Icons;
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
        private readonly WorldIconManager _worldIconManager;
        private readonly InteractionGizmoManager _interactionGizmoManager;
        private readonly ViewportManager _viewportManager;
        private readonly EditorState _editorState;
        private readonly ShortcutsManager _shortcutsManager;
        private readonly SceneTracker _sceneTracker;
        private readonly HandleAllocator _handleAllocator;
        private readonly ToolManager _toolManager;
        private readonly PickingManager _pickingManager;

        private bool _isEnabled;

        private bool _disposedValue;

        internal EditorManager()
        {
            _searchManager = new SearchManager();
            _selectionManager = new SelectionManager();

            _contextManager = new ContextManager();
            _windowManager = new EditorWindowManager();
            _worldIconManager = new WorldIconManager();
            _interactionGizmoManager = new InteractionGizmoManager();
            _viewportManager = new ViewportManager();
            _editorState = new EditorState();
            _shortcutsManager = new ShortcutsManager();
            _sceneTracker = new SceneTracker();
            _handleAllocator = new HandleAllocator();
            _toolManager = new ToolManager();
            _pickingManager = new PickingManager();

            _isEnabled = false;

            _windowManager.AddOpenWindow(new WorldAxisWindow());
            _windowManager.AddOpenWindow(new ToolsWindow());
            _windowManager.AddOpenWindow(new SceneWindow());

            InspectorManager inspectorManager = VoxelRuntime.Instance.InspectorManager;
            inspectorManager.OnInspectStart += (context) => _windowManager.AddOpenWindow(new InspectorWindow(context));

            _selectionManager.OnActiveEntityChanged += OnActiveEntityChangedCallback;
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
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void OnActiveEntityChangedCallback(SceneEntity oldEntity, SceneEntity newEntity)
        {
            InspectorManager inspectorManager = VoxelRuntime.Instance.InspectorManager;
            inspectorManager.StopInspect(ref oldEntity);
            inspectorManager.StartInspect<EntityInspectorContext, SceneEntity>(ref newEntity);
        }

        internal void Enable()
        {
            if (_isEnabled)
                return;

            VoxelRuntime runtime = VoxelRuntime.Instance;

            runtime.EventManager.AddHandler(_contextManager);
            runtime.RenderingManager.RenderPassManager.AddRenderPass<DearImGuiRenderPass>();
            runtime.RenderingManager.RenderPassManager.AddRenderPass<DrawWorldIconsPass>();
            runtime.RenderingManager.RenderPassManager.AddRenderPass<InteractionGizmoRenderPass>();
            runtime.RenderingManager.RenderPassManager.AddRenderPass<PickingRenderPass>();

            _isEnabled = true;
        }

        internal void Disable()
        {
            if (!_isEnabled)
                return;

            VoxelRuntime runtime = VoxelRuntime.Instance;

            runtime.EventManager.RemoveHandler(_contextManager);
            runtime.RenderingManager.RenderPassManager.RemoveRenderPass<DearImGuiRenderPass>();
            runtime.RenderingManager.RenderPassManager.RemoveRenderPass<DrawWorldIconsPass>();
            runtime.RenderingManager.RenderPassManager.RemoveRenderPass<InteractionGizmoRenderPass>();
            runtime.RenderingManager.RenderPassManager.RemoveRenderPass<PickingRenderPass>();

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

        public SearchManager SearchManager => _searchManager;
        public SelectionManager SelectionManager => _selectionManager;

        public static EditorManager Instance => VoxelRuntime.Instance.EditorManager;
    }
}
