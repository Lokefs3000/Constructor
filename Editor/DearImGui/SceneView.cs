using Editor.DearImGui.ViewWidgets;
using Editor.Gui;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Components;
using Primary.Input;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.RenderLayer;
using Primary.RHI;
using Primary.Scenes;
using Primary.Timing;
using System.Globalization;
using System.Numerics;

namespace Editor.DearImGui
{
    internal class SceneView : IDisposable
    {
        private GfxRenderTarget _outputRT;
        private SceneEntity _sceneEntity;

        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projMatrix;
        private Vector2 _viewRotation;

        private bool _isViewVisible;
        private bool _isViewActive;

        private Vector2 _localMouseHit;
        private Vector2 _relativeMouseHit;

        private Ray _cameraMouseRay;

        private float _cameraSpeedMult;

        private float _cameraSpeedMultTimer;

        private DynamicIconSet _widgetIconSet;
        private List<SceneViewWidget> _widgets;

        private bool _disposedValue;

        internal SceneView(DynamicAtlasManager atlasManager)
        {
            _outputRT = GfxDevice.Current.CreateRenderTarget(new RenderTargetDescription
            {
                Dimensions = new Size(1, 1),

                ColorFormat = RenderTargetFormat.RGB10A2un,
                DepthFormat = DepthStencilFormat.Undefined,

                ShaderVisibility = RenderTargetVisiblity.Color
            });
            _sceneEntity = SceneEntityManager.CreateEntity(null);

            _viewMatrix = Matrix4x4.Identity;

            _localMouseHit = Vector2.Zero;
            _relativeMouseHit = Vector2.Zero;

            _cameraMouseRay = default;

            _cameraSpeedMult = 1.0f;

            _cameraSpeedMultTimer = 0.0f;

            _widgetIconSet = atlasManager.CreateIconSet();
            _widgets = [
                new ToolSpaceWidget(),
                new ToolSelectorWidget(),
                new SnappingWidget(),
                new FramePacingWidget()
                ];

            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgets[i].SetIconSet(_widgetIconSet);
            }

            _sceneEntity.Enabled = false;
            _sceneEntity.SetComponent(new Camera
            {
                Clear = CameraClear.Solid,
                ClearColor = Color.Black,

                FieldOfView = 70.0f,
                NearClip = 0.02f,
                FarClip = 1000.0f
            });

            RenderingManager rendering = Editor.GlobalSingleton.RenderingManager;
            rendering.PreRender += PreRenderCallback;

            CollectWidgetIconPaths();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _outputRT.Dispose();

                    RenderingManager rendering = Editor.GlobalSingleton.RenderingManager;
                    rendering.PreRender -= PreRenderCallback;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void CollectWidgetIconPaths()
        {
            _widgetIconSet.Clear();

            for (int i = 0; i < _widgets.Count; i++)
            {
                _widgetIconSet.AddIcons(_widgets[i].RequiredIcons);
            }
        }

        private void PreRenderCallback()
        {
            if (!_isViewVisible)
                return;
            if (_outputRT.IsNull)
                return;

            Vector2 clientSize = _outputRT.Description.Dimensions.AsVector2();
            if (clientSize.X < 0.5f || clientSize.Y < 0.5f)
            {
                return;
            }

            _projMatrix = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(70.0f), clientSize.X / clientSize.Y, 0.02f, 1000.0f);

            ref Transform transform = ref _sceneEntity.GetComponent<Transform>();

            RenderingManager manager = Editor.GlobalSingleton.RenderingManager;
            manager.RenderScene.AddOutputViewport(new RSOutputViewport
            {
                Id = long.MaxValue,

                Target = _outputRT.RHIRenderTarget,
                ClientSize = clientSize,
                ViewMatrix = _viewMatrix,
                ProjectionMatrix = _projMatrix,

                ViewPosition = transform.Position,
                ViewDirection = Vector3.UnitZ,

                RootEntity = _sceneEntity.WrappedEntity,
            });
        }

        internal void Render()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            bool windowVis = ImGui.Begin("Scene view", ImGuiWindowFlags.MenuBar);

            _isViewVisible = windowVis;
            _isViewActive = ImGui.IsWindowHovered();

            ImGui.PopStyleVar();

            if (windowVis)
            {
                if (ImGui.BeginMenuBar())
                {
                    for (int i = 0; i < _widgets.Count; i++)
                    {
                        if (!_widgets[i].IsFloating)
                            _widgets[i].RenderSelf();
                    }

                    //if (ImGui.BeginMenu("Viewmode"))
                    //{
                    //    ref RenderingConfig rconfig = ref Editor.GlobalSingleton.RenderingManager.Configuration;
                    //
                    //    if (ImGui.MenuItem("Lit", rconfig.RenderMode == RenderingMode.Lit)) rconfig.RenderMode = RenderingMode.Lit;
                    //    if (ImGui.MenuItem("Unlit", rconfig.RenderMode == RenderingMode.Unlit)) rconfig.RenderMode = RenderingMode.Unlit;
                    //    if (ImGui.MenuItem("Wireframe", rconfig.RenderMode == RenderingMode.Wireframe)) rconfig.RenderMode = RenderingMode.Wireframe;
                    //    if (ImGui.MenuItem("Normals", rconfig.RenderMode == RenderingMode.Normals)) rconfig.RenderMode = RenderingMode.Normals;
                    //    if (ImGui.MenuItem("Lighting", rconfig.RenderMode == RenderingMode.Lighting)) rconfig.RenderMode = RenderingMode.Lighting;
                    //    if (ImGui.MenuItem("Detail lighting", rconfig.RenderMode == RenderingMode.DetailLighting)) rconfig.RenderMode = RenderingMode.DetailLighting;
                    //    if (ImGui.MenuItem("Reflections", rconfig.RenderMode == RenderingMode.Reflections)) rconfig.RenderMode = RenderingMode.Reflections;
                    //    if (ImGui.MenuItem("Shader complexity", rconfig.RenderMode == RenderingMode.ShaderComplexity)) rconfig.RenderMode = RenderingMode.ShaderComplexity;
                    //    if (ImGui.MenuItem("Overdraw", rconfig.RenderMode == RenderingMode.Overdraw)) rconfig.RenderMode = RenderingMode.Overdraw;
                    //
                    //    ImGui.EndMenu();
                    //}
                    //
                    //if (ImGui.MenuItem("Stats", _showQuickStats))
                    //    _showQuickStats = !_showQuickStats;
                }
                ImGui.EndMenuBar();

                Vector2 avail = Vector2.Max(ImGui.GetContentRegionAvail(), Vector2.One);
                if (avail.X > 0.5f && avail.Y > 0.5f)
                {
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
                }

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                ImGuiContextPtr context = ImGui.GetCurrentContext();

                Vector2 screenPos = ImGui.GetCursorScreenPos();
                Vector2 contentAvail = ImGui.GetContentRegionAvail();

                _localMouseHit = ImGui.GetMousePos() - screenPos;
                _relativeMouseHit = _localMouseHit / contentAvail * 2.0f - Vector2.One;

                _relativeMouseHit.Y = -_relativeMouseHit.Y;

                _cameraMouseRay = ExMath.ViewportToWorld(_projMatrix, _viewMatrix, _relativeMouseHit);

                if (!_outputRT.IsNull)
                    ImGui.Image(ImGuiUtility.GetTextureRef(_outputRT.ColorTexture.Handle), avail);
                ImGui.SameLine();

                if (_cameraSpeedMultTimer > 0.0f)
                {
                    string text = _cameraSpeedMult.ToString("F2", CultureInfo.InvariantCulture) + "x";

                    Vector2 center = screenPos + contentAvail * 0.5f;
                    Vector2 textSize = ImGui.CalcTextSize(text);

                    drawList.AddRectFilled(center - new Vector2(75.0f, 30.0f), center + new Vector2(75.0f, 30.0f), new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg].AsVector3(), _cameraSpeedMultTimer).ABGR, 4.0f);
                    unsafe
                    {
                        drawList.AddText(context.Font.Handle, context.FontSize * 2.0f, center - textSize, new Color32(1.0f, 1.0f, 1.0f, _cameraSpeedMultTimer).ABGR, text);
                    }

                    _cameraSpeedMultTimer -= Time.DeltaTime * 1.25f;
                }

                //if (_showQuickStats)
                //{
                //    Boundaries boundaries = new Boundaries(Vector2.Zero, new Vector2(230.0f, 70.0f));
                //    boundaries = Boundaries.Offset(boundaries, screenPos + new Vector2(contentAvail.X - context.Style.FramePadding.X - boundaries.Maximum.X, context.Style.FramePadding.Y * 2.0f));
                //
                //    uint id = ImGui.GetID("##STATS");
                //    ImRect bb = new ImRect(boundaries.Minimum, boundaries.Maximum);
                //
                //    long totalMem = GC.GetTotalMemory(false);
                //    GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
                //
                //    long allocRate = totalMem < _lastGCHeapSize ? ((memoryInfo.HeapSizeBytes - memoryInfo.FragmentedBytes) - _lastGCHeapSize) : (totalMem - _lastGCHeapSize);
                //    _lastGCHeapSize = totalMem;
                //
                //    ImGuiP.ItemAdd(bb, id);
                //    ImGuiP.ItemSize(bb);
                //
                //    drawList.AddRectFilled(boundaries.Minimum, boundaries.Maximum, new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg].AsVector3(), 0.3f).ABGR, context.Style.FrameRounding);
                //    drawList.AddRect(boundaries.Minimum, boundaries.Maximum, new Color32(context.Style.Colors[(int)ImGuiCol.Border].AsVector3(), 0.3f).ABGR, context.Style.FrameRounding);
                //
                //    drawList.PushClipRect(boundaries.Minimum + context.Style.FramePadding, boundaries.Maximum - context.Style.FramePadding);
                //
                //    drawList.AddText(boundaries.Minimum + context.Style.FramePadding, 0xffffffff, $"Frame time: {(Time.DeltaTimeDouble * 1000.0).ToString("F3", CultureInfo.InvariantCulture)}ms ({(1.0 / Time.DeltaTimeDouble).ToString("F1", CultureInfo.InvariantCulture)})");
                //    drawList.AddText(boundaries.Minimum + new Vector2(context.Style.FramePadding.X, context.Style.FramePadding.Y * 2.0f + context.FontSize), 0xffffffff, $"Garbage: {FileUtility.FormatSize(totalMem, "F4", CultureInfo.InvariantCulture)} ({FileUtility.FormatSize(allocRate, "F2", CultureInfo.InvariantCulture)}/s)");
                //
                //    drawList.PopClipRect();
                //}
            }
            ImGui.End();

            ImGui.PushID("SV"u8);

            for (int i = 0; i < _widgets.Count; i++)
            {
                if (_widgets[i].IsFloating)
                    _widgets[i].RenderSelf();
            }

            ImGui.PopID();

            ImGuiIOPtr io = ImGui.GetIO();

            //camera
            if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                ref Transform transform = ref _sceneEntity.GetComponent<Transform>();
                ref WorldTransform world = ref _sceneEntity.GetComponent<WorldTransform>();


                Vector2 drag = io.MouseDelta;
                if (drag.LengthSquared() > 0.0f)
                {
                    drag *= 0.2f;
                    _viewRotation = _viewRotation + drag;

                    if (_viewRotation.X > 360.0f) _viewRotation.X -= 360.0f;
                    if (_viewRotation.X < -360.0f) _viewRotation.X += 360.0f;
                    if (_viewRotation.Y > 360.0f) _viewRotation.Y -= 360.0f;
                    if (_viewRotation.Y < -360.0f) _viewRotation.Y += 360.0f;
                }

                Vector2 rads = Vector2.DegreesToRadians(_viewRotation);
                transform.Rotation = Quaternion.CreateFromYawPitchRoll(-rads.X, rads.Y, 0.0f);

                float cameraSpeed = 15.0f * _cameraSpeedMult * (io.KeyShift ? 2.0f : 1.0f);

                if (ImGui.IsKeyDown(ImGuiKey.W))
                    transform.Position += Vector3.Transform(Vector3.UnitZ, transform.Rotation) * cameraSpeed * Time.DeltaTime;
                if (ImGui.IsKeyDown(ImGuiKey.S))
                    transform.Position += Vector3.Transform(-Vector3.UnitZ, transform.Rotation) * cameraSpeed * Time.DeltaTime;
                if (ImGui.IsKeyDown(ImGuiKey.A))
                    transform.Position += Vector3.Transform(Vector3.UnitX, transform.Rotation) * cameraSpeed * Time.DeltaTime;
                if (ImGui.IsKeyDown(ImGuiKey.D))
                    transform.Position += Vector3.Transform(-Vector3.UnitX, transform.Rotation) * cameraSpeed * Time.DeltaTime;
                if (ImGui.IsKeyDown(ImGuiKey.E))
                    transform.Position += Vector3.Transform(Vector3.UnitY, transform.Rotation) * cameraSpeed * Time.DeltaTime;
                if (ImGui.IsKeyDown(ImGuiKey.Q))
                    transform.Position += Vector3.Transform(-Vector3.UnitY, transform.Rotation) * cameraSpeed * Time.DeltaTime;

                if (io.MouseWheel != 0.0f)
                {
                    _cameraSpeedMult = MathF.Min(MathF.Max(_cameraSpeedMult + io.MouseWheel * MathF.Min(_cameraSpeedMult * 0.12f, 0.2f), 0.01f), 2.0f);
                    _cameraSpeedMultTimer = 0.5f;
                }

                _viewMatrix = Matrix4x4.CreateLookTo(transform.Position, Vector3.Transform(Vector3.UnitZ, transform.Rotation), Vector3.Transform(Vector3.UnitY, transform.Rotation));
                //world.Transformation = Matrix4x4.CreateLookTo(transform.Position, Vector3.Transform(Vector3.UnitZ, transform.Rotation), Vector3.Transform(Vector3.UnitY, transform.Rotation));
                //world.UpdateIndex = Time.FrameIndex;
            }

            if (io.MouseClicked[(int)ImGuiMouseButton.Left])
                MouseDown?.Invoke(ImGuiMouseButton.Left);
            else if (io.MouseReleased[(int)ImGuiMouseButton.Left])
                MouseUp?.Invoke(ImGuiMouseButton.Left);

            if (io.MouseClicked[(int)ImGuiMouseButton.Middle])
                MouseDown?.Invoke(ImGuiMouseButton.Middle);
            else if (io.MouseReleased[(int)ImGuiMouseButton.Middle]) 
                MouseUp?.Invoke(ImGuiMouseButton.Middle);

            if (io.MouseClicked[(int)ImGuiMouseButton.Right])
                MouseDown?.Invoke(ImGuiMouseButton.Right);
            else if (io.MouseReleased[(int)ImGuiMouseButton.Right])
                MouseUp?.Invoke(ImGuiMouseButton.Right);

            if (io.MouseDelta.X != 0.0f || io.MouseDelta.Y != 0.0f)
                MouseMoved?.Invoke(_localMouseHit, _relativeMouseHit);

            if (InputSystem.Keyboard.IsAnyKeyPressed)
                KeyDown?.Invoke();
        }

        internal Vector2 LocalMouseHit => _localMouseHit;
        internal Vector2 RelativeMouseHit => _relativeMouseHit;

        internal bool IsViewActive => _isViewActive;
        internal bool IsViewVisible => _isViewVisible;

        internal Vector3 CameraTranslation => _sceneEntity.GetComponent<Transform>().Position;
        internal Vector3 CameraForward => new Vector3(_viewMatrix.M31, _viewMatrix.M32, _viewMatrix.M33);
        internal Vector2 OutputClientSize => _outputRT.Description.Dimensions.AsVector2();
        internal Matrix4x4 ViewMatrix => _viewMatrix;
        internal Matrix4x4 ProjectionMatrix => _projMatrix;

        internal Ray CameraMouseRay => _cameraMouseRay;

        internal event Action<ImGuiMouseButton>? MouseDown;
        internal event Action<ImGuiMouseButton>? MouseUp;
        internal event Action<Vector2, Vector2>? MouseMoved;

        internal event Action? KeyDown;

        private enum StatsDockPosition : byte
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }
    }
}
