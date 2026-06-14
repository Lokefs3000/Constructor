using Editor.Gui.View;
using Editor.Rendering.Debugging;
using Primary.Components;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.Profiling;
using Primary.Rendering;
using Primary.RHI;
using Primary.Windowing;
using System.Diagnostics;
using System.Numerics;

namespace Editor.Rendering
{
    public sealed class EditorRenderManager : IDisposable
    {
        private EntityDebugRenderer _entityDbgRenderer;

        private Gizmos _gizmos;
        private ScreenGizmos _screenGizmos;

        private RHITexture? _primaryOutputView;
        private Int2? _pendingViewResize;

        private EditorCamera _editorCamera;

        private bool _disposedValue;

        internal EditorRenderManager()
        {
            _entityDbgRenderer = new EntityDebugRenderer();

            _gizmos = new Gizmos();
            _screenGizmos = new ScreenGizmos();

            _editorCamera = new EditorCamera();

            RenderingManager renderer = EditorRuntime.GlobalSingleton.RenderingManager;
            renderer.RenderWorld.TransformOutput.Subscribe(OnTransformOutput);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    RenderingManager renderer = EditorRuntime.GlobalSingleton.RenderingManager;
                    renderer.RenderWorld.TransformOutput.Unsubscribe(OnTransformOutput);

                    _primaryOutputView?.Dispose();
                    _primaryOutputView = null;

                    _gizmos.Dispose();
                    _screenGizmos.Dispose();
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
            using (new ProfilingScope("RenderPrepareFrame"))
            {
                _gizmos.ClearDrawData();
                _screenGizmos.ClearDrawData();

                if (_pendingViewResize.HasValue)
                {
                    Int2 newSize = _pendingViewResize.Value;
                    _pendingViewResize = null;

                    if (_primaryOutputView == null)
                    {
                        if (newSize.X >= 1 && newSize.Y >= 1)
                            _primaryOutputView = CreateOutputView(newSize);
                    }
                    else
                    {
                        if (newSize.X < 1 || newSize.Y < 1)
                        {
                            _primaryOutputView.Dispose();
                            _primaryOutputView = null;
                        }
                        else if (new Int2(_primaryOutputView.Description.Width, _primaryOutputView.Description.Height) != newSize)
                        {
                            _primaryOutputView.Dispose();
                            _primaryOutputView = CreateOutputView(newSize);
                        }
                    }

                    _editorCamera.ClientSize = newSize.AsVector2();
                }

                _editorCamera.UpdateVectors();
            }

            _entityDbgRenderer.Render();
        }

        private void OnTransformOutput(List<RenderOutputData> outputs)
        {
            if (_primaryOutputView == null)
                return;

            Window? primaryWindow = WindowManager.Instance.PrimaryWindow;
            for (int i = outputs.Count - 1; i >= 0; --i)
            {
                RenderOutputData outputData = outputs[i];
                if (outputData.Window == primaryWindow && outputData.TargetTexture == null)
                {
                    Camera cameraData = outputData.Camera;
                    CameraProjectionData projectionData = new CameraProjectionData
                    {
                        ClientSize = _editorCamera.ClientSize,
                        ProjectionMatrix = _editorCamera.ProjectionMatrix,
                        ViewMatrix = _editorCamera.ViewMatrix
                    };

                    outputs[i] = new RenderOutputData(outputData.Entity, new WorldTransform { Transformation = _editorCamera.Transform, UpdateIndex = int.MinValue }, cameraData, projectionData, primaryWindow, _primaryOutputView);
                    break;
                }
            }
        }

        internal void UpdateViewSize(Int2 newSize) => _pendingViewResize = newSize;

        public EditorCamera Camera => _editorCamera;

        internal RHITexture? ViewTexture => _primaryOutputView;

        private static RHITexture? CreateOutputView(Int2 size)
        {
            return RHIDevice.Instance?.CreateTexture(new RHITextureDescription
            {
                Width = size.X,
                Height = size.Y,
                MipLevels = 1,

                Dimension = RHIDimension.Texture2D,
                Format = RHIFormat.RGB10A2_UNorm,
                Usage = RHIResourceUsage.RenderTarget | RHIResourceUsage.ShaderResource,

                Swizzle = new RHISwizzle(RHISwizzleChannel.Red, RHISwizzleChannel.Green, RHISwizzleChannel.Blue, RHISwizzleChannel.One)
            }, [], "SceneOutput");
        }
    }
}
