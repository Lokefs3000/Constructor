using Primary.Rendering.Resources;
using Primary.Scenes;
using System.Numerics;

namespace Primary.Rendering.Data
{
    public sealed class RenderCameraData : IContextItem
    {
        public SceneEntity CameraEntity { get; internal set; }

        public FrameGraphTexture ColorTexture { get; internal set; }
        public FrameGraphTexture DepthTexture { get; internal set; }

        public Matrix4x4 View { get; private set; }
        public Matrix4x4 Projection { get; private set; }
        public Matrix4x4 ViewProjection { get; private set; }

        internal RenderCameraData()
        {

        }

        internal void Setup(RenderOutputData outputData)
        {
            View = outputData.ProjectionData.ViewMatrix;
            Projection = outputData.ProjectionData.ProjectionMatrix;
            ViewProjection = outputData.ProjectionData.ViewMatrix * outputData.ProjectionData.ProjectionMatrix;
        }
    }
}
