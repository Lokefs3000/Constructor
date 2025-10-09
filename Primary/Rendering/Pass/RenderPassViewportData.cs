using Primary.Components;
using System.Numerics;

namespace Primary.Rendering.Pass
{
    public sealed class RenderPassViewportData : IRenderPassDataObject
    {
        private RHI.RenderTarget _cameraRenderTarget = default!;
        private RHI.RenderTarget _backBufferRenderTarget = default!;

        private Camera _camera;
        private Matrix4x4 _view;
        private Matrix4x4 _projection;
        private Matrix4x4 _vp;

        private Vector3 _viewPos;
        private Vector3 _viewDir;

        public RHI.RenderTarget CameraRenderTarget { get => _cameraRenderTarget; internal set => _cameraRenderTarget = value; }
        public RHI.RenderTarget BackBufferRenderTarget { get => _backBufferRenderTarget; internal set => _backBufferRenderTarget = value; }

        public ref readonly Camera Camera { get => ref _camera; }
        internal Camera RefCameraSetter { set => _camera = value; }
        public Matrix4x4 View { get => _view; internal set => _view = value; }
        public Matrix4x4 Projection { get => _projection; internal set => _projection = value; }
        public Matrix4x4 VP { get => _vp; internal set => _vp = value; }

        public Vector3 ViewPosition { get => _viewPos; internal set => _viewPos = value; }
        public Vector3 ViewDirection { get => _viewDir; internal set => _viewDir = value; }
    }
}
