using Primary.Components;
using Primary.Mathematics;
using Primary.Rendering.Ancient;
using Primary.Rendering.Ancient.Resources;
using Primary.Scenes;
using System.Numerics;

namespace Primary.Rendering.Ancient.Data
{
    public sealed class RenderCameraData : IContextItem
    {
        public SceneEntity CameraEntity { get; private set; }

        public WorldTransform Transform { get; private set; }

        public FGTexture ColorTexture { get; internal set; }
        public FGTexture DepthTexture { get; internal set; }

        public Matrix4x4 View { get; private set; }
        public Matrix4x4 Projection { get; private set; }
        public Matrix4x4 ViewProjection { get; private set; }

        public Frustrum ViewFrustrum { get; private set; }

        public float ZNear { get; private set; }
        public float ZFar { get; private set; }

        public Int2 ClientSize { get; private set; }

        internal RenderCameraData()
        {

        }

        internal void Setup(RenderOutputData outputData)
        {
            CameraEntity = outputData.Entity;

            Transform = outputData.Transform;

            View = outputData.ProjectionData.ViewMatrix;
            Projection = outputData.ProjectionData.ProjectionMatrix;
            ViewProjection = outputData.ProjectionData.ViewMatrix * outputData.ProjectionData.ProjectionMatrix;

            ViewFrustrum = new Frustrum(ViewProjection);

            ZFar = outputData.Camera.FarClip;
            ZNear = outputData.Camera.NearClip;

            ClientSize = outputData.ProjectionData.ClientSize.AsInt2();
        }
    }
}
