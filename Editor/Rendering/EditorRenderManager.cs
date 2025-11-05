using Editor.Rendering.Debugging;
using Primary.Rendering;

namespace Editor.Rendering
{
    internal sealed class EditorRenderManager : IDisposable
    {
        private Editor _editor;

        private GizmoRenderPass _gizmoPass;
        private ToolsRenderPass _toolsPass;
        private SelectionRenderPass _selectionRenderPass;
        private GeoToolRenderPass _geoToolRenderPass;

        private Gizmos _gizmos;

        private EntityDebugRenderer _entityDbgRenderer;
        private StructureDebugRenderer _structureDbgRenderer;

        private bool _disposedValue;

        internal EditorRenderManager()
        {
            _editor = Editor.GlobalSingleton;

            _gizmoPass = new GizmoRenderPass();
            _toolsPass = new ToolsRenderPass();
            _selectionRenderPass = new SelectionRenderPass();
            _geoToolRenderPass = new GeoToolRenderPass();

            _gizmos = new Gizmos();

            _entityDbgRenderer = new EntityDebugRenderer();
            _structureDbgRenderer = new StructureDebugRenderer();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _gizmos.Dispose();

                    _geoToolRenderPass.Dispose();
                    _selectionRenderPass.Dispose();
                    _toolsPass.Dispose();
                    _gizmoPass.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void PrepareFrame()
        {
            _gizmos.ResetForNewFrame();
        }

        internal void SetupPasses(RenderPass renderPass)
        {
            _entityDbgRenderer.Render();
            _structureDbgRenderer.Render();

            _gizmos.FinalizeBuffers();

            _gizmoPass.SetupRenderState(renderPass);
            _selectionRenderPass.SetupRenderState(renderPass);
            _geoToolRenderPass.SetupRenderState(renderPass);
            _toolsPass.SetupRenderState(renderPass);
            _editor.DearImGuiStateManager.SetupPasses(renderPass);
        }

        internal Gizmos Gizmos => _gizmos;
    }
}
