using Editor.Interaction;
using Primary.Rendering;
using Primary.Rendering.Data;
using Primary.Rendering.Raw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Rendering
{
    internal sealed class ToolsRenderPass : IDisposable
    {
        private bool _disposedValue;

        internal ToolsRenderPass()
        {

        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void SetupRenderState(RenderPass renderPass)
        {
            using (RasterPassDescription desc = renderPass.CreateRasterPass())
            {
                desc.SetThreadingPolicy(RenderPassThreadingPolicy.None);
                desc.SetFunction(PassFunction);
            }
        }

        private void PassFunction(RasterCommandBuffer commandBuffer, RenderPassData passData)
        {
            ToolManager tools = Editor.GlobalSingleton.ToolManager;
            switch (tools.Tool)
            {
                case EditorTool.Translate: DrawTranslateGizmo(Vector3.Zero); break;
                case EditorTool.Rotate: DrawRotateGizmo(Vector3.Zero); break;
                case EditorTool.Scale: DrawScaleGizmo(Vector3.Zero); break;
            }
        }

        private void DrawTranslateGizmo(Vector3 origin)
        {

        }

        private void DrawRotateGizmo(Vector3 origin)
        {

        }

        private void DrawScaleGizmo(Vector3 origin)
        {

        }
    }
}
