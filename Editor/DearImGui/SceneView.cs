using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Rendering;
using Primary.RenderLayer;
using Primary.RHI;
using System.Numerics;

namespace Editor.DearImGui
{
    internal class SceneView : IDisposable
    {
        private GfxRenderTarget _outputRT;

        private bool disposedValue;

        internal SceneView()
        {
            _outputRT = GfxRenderTarget.Null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _outputRT.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Render()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            bool windowVis = ImGui.Begin("Scene view", ImGuiWindowFlags.MenuBar);

            ImGui.PopStyleVar();

            if (windowVis)
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Viewmode"))
                    {
                        ref RenderingConfig rconfig = ref Editor.GlobalSingleton.RenderingManager.Configuration;

                        if (ImGui.MenuItem("Lit", rconfig.RenderMode == RenderingMode.Lit)) rconfig.RenderMode = RenderingMode.Lit;
                        if (ImGui.MenuItem("Unlit", rconfig.RenderMode == RenderingMode.Unlit)) rconfig.RenderMode = RenderingMode.Unlit;
                        if (ImGui.MenuItem("Wireframe", rconfig.RenderMode == RenderingMode.Wireframe)) rconfig.RenderMode = RenderingMode.Wireframe;
                        if (ImGui.MenuItem("Normals", rconfig.RenderMode == RenderingMode.Normals)) rconfig.RenderMode = RenderingMode.Normals;
                        if (ImGui.MenuItem("Lighting", rconfig.RenderMode == RenderingMode.Lighting)) rconfig.RenderMode = RenderingMode.Lighting;
                        if (ImGui.MenuItem("Detail lighting", rconfig.RenderMode == RenderingMode.DetailLighting)) rconfig.RenderMode = RenderingMode.DetailLighting;
                        if (ImGui.MenuItem("Reflections", rconfig.RenderMode == RenderingMode.Reflections)) rconfig.RenderMode = RenderingMode.Reflections;
                        if (ImGui.MenuItem("Shader complexity", rconfig.RenderMode == RenderingMode.ShaderComplexity)) rconfig.RenderMode = RenderingMode.ShaderComplexity;
                        if (ImGui.MenuItem("Overdraw", rconfig.RenderMode == RenderingMode.Overdraw)) rconfig.RenderMode = RenderingMode.Overdraw;
                    
                        ImGui.EndMenu();
                    }
                }
                ImGui.EndMenuBar();

                Vector2 avail = ImGui.GetContentRegionAvail();
                if (_outputRT.IsNull || _outputRT.Description.Dimensions.Width != avail.X || _outputRT.Description.Dimensions.Height != avail.Y)
                {
                    _outputRT.Dispose();
                    _outputRT = GfxDevice.Current.CreateRenderTarget(new RenderTargetDescription
                    {
                        Dimensions = new Size((int)avail.X, (int)avail.Y),

                        ColorFormat = RenderTargetFormat.RGB10A2un,
                        DepthFormat = DepthStencilFormat.Undefined,

                        ShaderVisibility = RenderTargetVisiblity.Color
                    });

                    ref RenderingConfig rconfig = ref Editor.GlobalSingleton.RenderingManager.Configuration;
                    rconfig.OutputRenderTarget = _outputRT;
                    rconfig.OutputViewport = avail;
                }

                ImGui.Image(ImGuiUtility.GetTextureRef(_outputRT.ColorTexture.Handle), avail);
            }
            ImGui.End();
        }
    }
}
