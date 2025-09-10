using Editor.Rendering.Gizmos;
using Primary.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Rendering
{
    internal sealed class EditorRenderManager : IDisposable
    {
        private Editor _editor;

        private GizmoRenderPass _gizmoPass;
        private ToolsRenderPass _toolsPass;
        private SelectionRenderPass _selectionRenderPass;

        private bool _disposedValue;

        internal EditorRenderManager()
        {
            _editor = Editor.GlobalSingleton;

            _gizmoPass = new GizmoRenderPass();
            _toolsPass = new ToolsRenderPass();
            _selectionRenderPass = new SelectionRenderPass();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
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

        internal void SetupPasses(RenderPass renderPass)
        {
            _selectionRenderPass.SetupRenderState(renderPass);
            _gizmoPass.ExecutePass(renderPass);
            _toolsPass.SetupRenderState(renderPass);
            _editor.DearImGuiStateManager.SetupPasses(renderPass);
        }
    }
}
