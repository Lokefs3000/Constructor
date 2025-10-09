using Arch.LowLevel;
using Editor.DearImGui;
using Editor.Interaction;
using Editor.Interaction.Controls;
using Editor.Interaction.Tools;
using Primary.Assets;
using Primary.Components;
using Primary.Rendering;
using Primary.Rendering.Pass;
using Primary.Rendering.Raw;
using Silk.NET.Assimp;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using RHI = Primary.RHI;

namespace Editor.Rendering
{
    internal sealed class ToolsRenderPass : IDisposable
    {
        private bool _disposedValue;

        private UnsafeList<Point> _lines;
        private UnsafeList<Triangle> _triangles;
        private UnsafeList<UVTriangle> _uvTriangles;

        private ShaderAsset _lineShader;
        private ShaderAsset _triangleShader;
        private ShaderAsset _numericShader;

        private TextureAsset _numericTexture;

        private ShaderBindGroup _bindGroup;
        private ShaderBindGroup _numericBG;

        private RHI.Buffer _vertexData;

        private RHI.Buffer? _vertexBuffer;
        private int _vertexBufferSize;

        internal ToolsRenderPass()
        {
            _lines = new UnsafeList<Point>(16);
            _triangles = new UnsafeList<Triangle>(6);
            _uvTriangles = new UnsafeList<UVTriangle>(6);

            _lineShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/ToolsLine.hlsl", true);
            _triangleShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/ToolsTriangle.hlsl", true);
            _numericShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/ToolsNumerics.hlsl", true);

            _numericTexture = AssetManager.LoadAsset<TextureAsset>("Editor/Textures/ToolNumerical.png", true);

            _bindGroup = new ShaderBindGroup(null, new ShaderBindGroupVariable(ShaderVariableType.ConstantBuffer, "cbVertex"));
            _numericBG = _numericShader.CreateDefaultBindGroup();

            _vertexData = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<Matrix4x4>(),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.None,
                Stride = (uint)Unsafe.SizeOf<Matrix4x4>(),
                Usage = RHI.BufferUsage.ConstantBuffer
            }, nint.Zero);

            _vertexBuffer = null;
            _vertexBufferSize = 0;

            _bindGroup.SetResource("cbVertex", _vertexData);
            _numericBG.SetResource("txNumbers", _numericTexture);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _vertexBuffer?.Dispose();
                    _vertexBufferSize = 0;

                    _vertexData.Dispose();

                    _lines.Dispose();
                    _triangles.Dispose();
                    _uvTriangles.Dispose();
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
            _lines.Clear();
            _triangles.Clear();
            _uvTriangles.Clear();

            IControlTool controlTool = Editor.GlobalSingleton.ToolManager.ActiveControlTool;

            Vector3 absoluteMin = Vector3.PositiveInfinity;
            Vector3 absoluteMax = Vector3.NegativeInfinity;

            Quaternion baseQuat = Quaternion.Identity;

            bool hasValidSelections = false;
            foreach (ref readonly IToolTransform transform in controlTool.Transforms)
            {
                absoluteMin = Vector3.Min(absoluteMin, transform.WorldMatrix.Translation);
                absoluteMax = Vector3.Max(absoluteMax, transform.WorldMatrix.Translation);

                if (!hasValidSelections)
                {
                    Matrix4x4.Decompose(transform.WorldMatrix, out _, out baseQuat, out _);
                }

                hasValidSelections = true;
            }

            if (!hasValidSelections)
                return;

            Vector3 center = Vector3.Lerp(absoluteMin, absoluteMax, 0.5f);
            Matrix4x4 lookMatrix = Matrix4x4.CreateFromQuaternion(baseQuat) * Matrix4x4.CreateTranslation(center);

            RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;

            DrawSelectedObjectsGizmos(center);

            ToolManager tools = Editor.GlobalSingleton.ToolManager;
            switch (tools.Tool)
            {
                case EditorTool.Translate: DrawTranslateGizmo(center, lookMatrix, viewportData); break;
                case EditorTool.Rotate: DrawRotateGizmo(center, lookMatrix, viewportData); break;
                case EditorTool.Scale: DrawScaleGizmo(center, lookMatrix, viewportData); break;
            }

            RenderNewGeometry(commandBuffer, lookMatrix, viewportData);
        }

        private void RenderNewGeometry(RasterCommandBuffer commandBuffer, Matrix4x4 lookMatrix, RenderPassViewportData viewportData)
        {
            if (_lines.Count == 0 && _triangles.Count == 0)
                return;

            Vector2 size = viewportData.BackBufferRenderTarget.Description.Dimensions.AsVector2();
            for (int i = 0; i < 1; i++)
            {
                {
                    
                    Span<Matrix4x4> mapped = commandBuffer.Map<Matrix4x4>(_vertexData, RHI.MapIntent.Write);
                    mapped[0] = i == 0 ? lookMatrix * viewportData.VP : Matrix4x4.CreateOrthographic(size.X, size.Y, -1.0f, 1.0f);
                    commandBuffer.Unmap(_vertexData);
                }

                int lineDataSize = _lines.Count * Unsafe.SizeOf<Point>();
                int triangleDataSize = _triangles.Count * Unsafe.SizeOf<Triangle>();
                int uvTriangleDataSize = _uvTriangles.Count * Unsafe.SizeOf<UVTriangle>();

                int max = Math.Max(Math.Max(lineDataSize, triangleDataSize), uvTriangleDataSize);
                if (_vertexBuffer == null || _vertexBufferSize < max)
                {
                    _vertexBuffer?.Dispose();
                    _vertexBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                    {
                        ByteWidth = (uint)max,
                        Stride = 0,
                        Memory = RHI.MemoryUsage.Dynamic,
                        CpuAccessFlags = RHI.CPUAccessFlags.Write,
                        Mode = RHI.BufferMode.None,
                        Usage = RHI.BufferUsage.VertexBuffer
                    }, nint.Zero);
                    _vertexBufferSize = max;
                }

                commandBuffer.SetRenderTarget(viewportData.BackBufferRenderTarget);
                commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 10000, 10000));

                //commandBuffer.SetBindGroups(_bindGroup);
                commandBuffer.SetConstants(lookMatrix * viewportData.VP);

                //lines
                if (_lines.Count > 0)
                {
                    Span<Point> mapped = commandBuffer.Map<Point>(_vertexBuffer, RHI.MapIntent.Write, _lines.Count);
                    _lines.AsSpan().CopyTo(mapped);
                    commandBuffer.Unmap(_vertexBuffer);

                    commandBuffer.SetShader(_lineShader);
                    commandBuffer.CommitShaderResources();

                    commandBuffer.SetVertexBuffer(0, _vertexBuffer, (uint)Unsafe.SizeOf<Point>());
                    commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs((uint)_lines.Count));
                }

                //triangles
                if (_triangles.Count > 0)
                {
                    Span<Triangle> mapped = commandBuffer.Map<Triangle>(_vertexBuffer, RHI.MapIntent.Write, _triangles.Count);
                    _triangles.AsSpan().CopyTo(mapped);
                    commandBuffer.Unmap(_vertexBuffer);

                    commandBuffer.SetShader(_triangleShader);
                    commandBuffer.CommitShaderResources();

                    commandBuffer.SetVertexBuffer(0, _vertexBuffer, (uint)Unsafe.SizeOf<Point>());
                    commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs((uint)_triangles.Count * 3));
                }

                //uv triangles
                if (_uvTriangles.Count > 0)
                {
                    Span<UVTriangle> mapped = commandBuffer.Map<UVTriangle>(_vertexBuffer, RHI.MapIntent.Write, _uvTriangles.Count);
                    _uvTriangles.AsSpan().CopyTo(mapped);
                    commandBuffer.Unmap(_vertexBuffer);

                    commandBuffer.SetShader(_numericShader);
                    commandBuffer.SetBindGroups(_numericBG);
                    commandBuffer.CommitShaderResources();

                    commandBuffer.SetVertexBuffer(0, _vertexBuffer, (uint)Unsafe.SizeOf<UVPoint>());
                    commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs((uint)_uvTriangles.Count * 3));
                }
            }
        }

        private void DrawSelectedObjectsGizmos(Vector3 center)
        {
            SelectionManager selection = Editor.GlobalSingleton.SelectionManager;
            foreach (SelectedBase @base in selection.Selection)
            {
                if (@base is SelectedSceneEntity selected)
                {
                    ref WorldTransform transform = ref selected.Entity.GetComponent<WorldTransform>();
                    if (!Unsafe.IsNullRef(ref transform))
                    {
                        Vector3 root = transform.Transformation.Translation - center;

                        AddLine(root, root + new Vector3(0.5f, 0.0f, 0.0f), 0xffff0000);
                        AddLine(root, root + new Vector3(0.0f, 0.5f, 0.0f), 0xff00ff00);
                        AddLine(root, root + new Vector3(0.0f, 0.0f, 0.5f), 0xff0000ff);
                    }
                }
            }
        }

        private void DrawTranslateGizmo(Vector3 origin, Matrix4x4 lookMatrix, RenderPassViewportData viewportData)
        {
            Vector3 relative = Vector3.Transform(viewportData.ViewPosition - origin, Matrix4x4.Transpose(lookMatrix));

            bool negX = relative.X < 0.0f;
            bool negY = relative.Y < 0.0f;
            bool negZ = relative.Z < 0.0f;

            float scale = 1.0f; //MathF.Max(MathF.Min(Vector3.Distance(origin, viewportData.ViewPosition) * 0.1f, 1.5f), 0.25f);

            float shortLength = 3.0f * scale;
            float longLength = 3.5f * scale;

            float startLength = scale * 0.75f;

            Vector2 clientSize = viewportData.BackBufferRenderTarget.Description.Dimensions.AsVector2() * 0.5f;

            Vector2 mouseHit = Editor.GlobalSingleton.SceneView.LocalMouseHit;
            mouseHit = new Vector2(mouseHit.X - clientSize.X, clientSize.Y - mouseHit.Y);

            //center
            {
                float xValue = (negX ? -startLength : startLength);
                float yValue = (negY ? -startLength : startLength);
                float zValue = (negZ ? -startLength : startLength);

                AddLine(new Vector3(0.0f, 0.0f, zValue), new Vector3(xValue, 0.0f, zValue), 0xff808080);
                AddLine(new Vector3(xValue, 0.0f, zValue), new Vector3(xValue, 0.0f, 0.0f), 0xff808080);

                AddLine(new Vector3(0.0f, 0.0f, zValue), new Vector3(0.0f, yValue, zValue), 0xff808080);
                AddLine(new Vector3(xValue, 0.0f, 0.0f), new Vector3(xValue, yValue, 0.0f), 0xff808080);

                AddLine(new Vector3(0.0f, yValue, zValue), new Vector3(0.0f, yValue, 0.0f), 0xff808080);
                AddLine(new Vector3(xValue, yValue, 0.0f), new Vector3(0.0f, yValue, 0.0f), 0xff808080);
            }

            //x axis
            {

                float xValue = (negX ? -shortLength : longLength);
                float yValue = (negY ? -0.25f : 0.25f) * scale;
                float zValue = (negZ ? -0.25f : 0.25f) * scale;

                Vector3 hit1 = Vector3.Transform(new Vector3(negX ? -startLength : startLength, 0.0f, 0.0f), lookMatrix);
                Vector3 hit2 = Vector3.Transform(new Vector3(negX ? -shortLength : shortLength, yValue + yValue, 0.0f), lookMatrix);
                Vector3 hit3 = Vector3.Transform(new Vector3(negX ? -longLength : longLength, 0.0f, 0.0f), lookMatrix);
                Vector3 hit4 = Vector3.Transform(new Vector3(negX ? -shortLength : shortLength, 0.0f, zValue + zValue), lookMatrix);

                bool hovered = ScreenRectDetection(viewportData.VP, clientSize, mouseHit, hit1, hit2, hit3, hit4);
                uint color = hovered ? 0xffffff00 : 0xffff0000;

                AddLine(new Vector3(negX ? -startLength : startLength, 0.0f, 0.0f), new Vector3(xValue, 0.0f, 0.0f), color);

                Matrix4x4 lookAt = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateBillboard(
                    new Vector3(xValue * 1.35f, 0.0f, 0.0f),
                    viewportData.ViewPosition,
                    Vector3.UnitY,
                    Vector3.UnitZ);

                Vector3 charP1 = Vector3.Transform(new Vector3(-0.1f, -0.2f, 0.0f), lookAt);
                Vector3 charP2 = Vector3.Transform(new Vector3(0.1f, -0.2f, 0.0f), lookAt);
                Vector3 charP3 = Vector3.Transform(new Vector3(-0.1f, 0.2f, 0.0f), lookAt);
                Vector3 charP4 = Vector3.Transform(new Vector3(0.1f, 0.2f, 0.0f), lookAt);

                AddLine(charP1, charP4, color);
                AddLine(charP2, charP3, color);

                if (negX)
                {
                    Vector3 negP1 = Vector3.Transform(new Vector3(-0.2f, 0.0f, 0.0f), lookAt);
                    Vector3 negP2 = Vector3.Transform(new Vector3(-0.4f, 0.0f, 0.0f), lookAt);

                    AddLine(negP1, negP2, color);

                    AddLine(new Vector3(-shortLength, 0.0f, 0.0f), new Vector3(-shortLength, yValue, 0.0f), color);
                    AddLine(new Vector3(-shortLength, 0.0f, 0.0f), new Vector3(-shortLength, 0.0f, zValue), color);

                    AddLine(new Vector3(-shortLength, yValue, 0.0f), new Vector3(-longLength, 0.0f, 0.0f), color);
                    AddLine(new Vector3(-shortLength, 0.0f, zValue), new Vector3(-longLength, 0.0f, 0.0f), color);
                }
                else
                {
                    AddTriangle(new Vector3(shortLength, 0.0f, 0.0f), new Vector3(shortLength, yValue, 0.0f), new Vector3(longLength, 0.0f, 0.0f), color);
                    AddTriangle(new Vector3(shortLength, 0.0f, 0.0f), new Vector3(shortLength, 0.0f, zValue), new Vector3(longLength, 0.0f, 0.0f), color);
                }
            }

            //y axis
            {
                float xValue = (negX ? -0.25f : 0.25f) * scale;
                float yValue = (negY ? -shortLength : longLength);
                float zValue = (negZ ? -0.25f : 0.25f) * scale;

                Vector3 hit1 = Vector3.Transform(new Vector3(0.0f, negY ? -startLength : startLength, 0.0f), lookMatrix);
                Vector3 hit2 = Vector3.Transform(new Vector3(xValue + xValue, negY ? -shortLength : shortLength, 0.0f), lookMatrix);
                Vector3 hit3 = Vector3.Transform(new Vector3(0.0f, negY ? -longLength : longLength, 0.0f), lookMatrix);
                Vector3 hit4 = Vector3.Transform(new Vector3(0.0f, negY ? -shortLength : shortLength, zValue + zValue), lookMatrix);

                bool hovered = ScreenRectDetection(viewportData.VP, clientSize, mouseHit, hit1, hit2, hit3, hit4);
                uint color = hovered ? 0xffffff00 : 0xff00ff00;

                AddLine(new Vector3(0.0f, negY ? -startLength : startLength, 0.0f), new Vector3(0.0f, yValue, 0.0f), color);

                Matrix4x4 lookAt = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateBillboard(
                    new Vector3(0.0f, yValue * 1.35f, 0.0f),
                    viewportData.ViewPosition,
                    Vector3.UnitY,
                    Vector3.UnitZ);

                Vector3 charP1 = Vector3.Transform(new Vector3(-0.1f, 0.2f, 0.0f), lookAt);
                Vector3 charP2 = Vector3.Transform(new Vector3(0.1f, 0.2f, 0.0f), lookAt);
                Vector3 charP3 = Vector3.Transform(new Vector3(0.0f, 0.0f, 0.0f), lookAt);
                Vector3 charP4 = Vector3.Transform(new Vector3(0.0f, -0.2f, 0.0f), lookAt);

                AddLine(charP1, charP3, color);
                AddLine(charP2, charP3, color);
                AddLine(charP3, charP4, color);

                if (negY)
                {
                    Vector3 negP1 = Vector3.Transform(new Vector3(-0.2f, 0.0f, 0.0f), lookAt);
                    Vector3 negP2 = Vector3.Transform(new Vector3(-0.4f, 0.0f, 0.0f), lookAt);

                    AddLine(negP1, negP2, color);

                    AddLine(new Vector3(0.0f, -shortLength, 0.0f), new Vector3(0.0f, -shortLength, zValue), color);
                    AddLine(new Vector3(0.0f, -shortLength, 0.0f), new Vector3(xValue, -shortLength, 0.0f), color);

                    AddLine(new Vector3(0.0f, -shortLength, zValue), new Vector3(0.0f, -longLength, 0.0f), color);
                    AddLine(new Vector3(xValue, -shortLength, 0.0f), new Vector3(0.0f, -longLength, 0.0f), color);
                }
                else
                {
                    AddTriangle(new Vector3(0.0f, shortLength, 0.0f), new Vector3(0.0f, shortLength, zValue), new Vector3(0.0f, longLength, 0.0f), color);
                    AddTriangle(new Vector3(0.0f, shortLength, 0.0f), new Vector3(xValue, shortLength, 0.0f), new Vector3(0.0f, longLength, 0.0f), color);
                }
            }

            //z axis
            {
                float xValue = (negX ? -0.25f : 0.25f) * scale;
                float yValue = (negY ? -0.25f : 0.25f) * scale;
                float zValue = (negZ ? -shortLength : longLength);

                Vector3 hit1 = Vector3.Transform(new Vector3(0.0f, 0.0f, negZ ? -startLength : startLength), lookMatrix);
                Vector3 hit2 = Vector3.Transform(new Vector3(xValue + xValue, 0.0f, negZ ? -shortLength : shortLength), lookMatrix);
                Vector3 hit3 = Vector3.Transform(new Vector3(0.0f, 0.0f, negZ ? -longLength : longLength), lookMatrix);
                Vector3 hit4 = Vector3.Transform(new Vector3(0.0f, yValue + yValue, negZ ? -shortLength : shortLength), lookMatrix);

                bool hovered = ScreenRectDetection(viewportData.VP, clientSize, mouseHit, hit1, hit2, hit3, hit4);
                uint color = hovered ? 0xffffff00 : 0xff0000ff;

                AddLine(new Vector3(0.0f, 0.0f, negZ ? -startLength : startLength), new Vector3(0.0f, 0.0f, zValue), color);

                Matrix4x4 lookAt = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateBillboard(
                    new Vector3(0.0f, 0.0f, zValue * 1.35f),
                    viewportData.ViewPosition,
                    Vector3.UnitY,
                    Vector3.UnitZ);

                Vector3 charP1 = Vector3.Transform(new Vector3(-0.1f, 0.2f, 0.0f), lookAt);
                Vector3 charP2 = Vector3.Transform(new Vector3(0.1f, 0.2f, 0.0f), lookAt);
                Vector3 charP3 = Vector3.Transform(new Vector3(-0.1f, -0.2f, 0.0f), lookAt);
                Vector3 charP4 = Vector3.Transform(new Vector3(0.1f, -0.2f, 0.0f), lookAt);

                AddLine(charP1, charP2, color);
                AddLine(charP1, charP4, color);
                AddLine(charP3, charP4, color);

                if (negZ)
                {
                    Vector3 negP1 = Vector3.Transform(new Vector3(-0.2f, 0.0f, 0.0f), lookAt);
                    Vector3 negP2 = Vector3.Transform(new Vector3(-0.4f, 0.0f, 0.0f), lookAt);

                    AddLine(negP1, negP2, color);

                    AddLine(new Vector3(0.0f, 0.0f, -shortLength), new Vector3(0.0f, yValue, -shortLength), color);
                    AddLine(new Vector3(0.0f, 0.0f, -shortLength), new Vector3(xValue, 0.0f, -shortLength), color);

                    AddLine(new Vector3(0.0f, yValue, -shortLength), new Vector3(0.0f, 0.0f, -longLength), color);
                    AddLine(new Vector3(xValue, 0.0f, -shortLength), new Vector3(0.0f, 0.0f, -longLength), color);
                }
                else
                {
                    AddTriangle(new Vector3(0.0f, 0.0f, shortLength), new Vector3(0.0f, yValue, shortLength), new Vector3(0.0f, 0.0f, longLength), color);
                    AddTriangle(new Vector3(0.0f, 0.0f, shortLength), new Vector3(xValue, 0.0f, shortLength), new Vector3(0.0f, 0.0f, longLength), color);
                }
            }

            TranslateTool? tool = Editor.GlobalSingleton.ToolManager.ToolObject as TranslateTool;
            if (tool != null && tool.IsActive)
            {
                SceneView sceneView = Editor.GlobalSingleton.SceneView;

                uint color = tool.Axis switch
                {
                    TranslateTool.DragAxis.X => 0xffff8080,
                    TranslateTool.DragAxis.Y => 0xff80ff80,
                    TranslateTool.DragAxis.Z => 0xff8080ff,
                    _ => 0xffff00ff
                };

                Vector3 position = tool.Axis switch
                {
                    TranslateTool.DragAxis.X => negX ? -Vector3.UnitX : Vector3.UnitX,
                    TranslateTool.DragAxis.Y => negY ? -Vector3.UnitY : Vector3.UnitY,
                    TranslateTool.DragAxis.Z => negZ ? -Vector3.UnitZ : Vector3.UnitZ,
                    _ => Vector3.Zero
                } * 1.25f;

                string txt = string.Empty;
                float dt = tool.Delta;

                if (ToolManager.IsSnappingActive)
                {
                    float snapScale = ToolManager.SnapScale;
                    dt = MathF.Round(dt / snapScale) * snapScale;
                }

                if (float.IsNegativeInfinity(dt))
                    txt = "-∞";
                else if (float.IsPositiveInfinity(dt))
                    txt = "+∞";
                else
                    txt = dt.ToString(CultureInfo.InvariantCulture);

                AddNumbers(origin, position, txt, color);
            }
        }

        private void DrawRotateGizmo(Vector3 origin, Matrix4x4 lookMatrix, RenderPassViewportData viewportData)
        {

        }

        private void DrawScaleGizmo(Vector3 origin, Matrix4x4 lookMatrix, RenderPassViewportData viewportData)
        {

        }

        private void AddLine(Vector3 from, Vector3 to, uint color)
        {
            _lines.Add(new Point(from, color));
            _lines.Add(new Point(to, color));
        }

        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, uint color)
        {
            _triangles.Add(new Triangle(
                new Point(a, color),
                new Point(b, color),
                new Point(c, color)));
        }

        private void AddUVTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC, uint color)
        {
            _uvTriangles.Add(new UVTriangle(
                new UVPoint(a, uvA, color),
                new UVPoint(b, uvB, color),
                new UVPoint(c, uvC, color)));
        }

        private void AddNumbers(Vector3 realPos, Vector3 localPos, string text, uint color)
        {
            Matrix4x4 billboard = Matrix4x4.CreateBillboard(realPos + localPos, Editor.GlobalSingleton.SceneView.CameraTranslation, Vector3.UnitY, Vector3.UnitZ);
            billboard.Translation = localPos;
            billboard = Matrix4x4.CreateScale(0.25f) * billboard;

            Vector3 pos = Vector3.Zero;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                ref NumberSheetInfo info = ref s_numberSheetData[c switch
                {
                    '.' => 10,
                    '-' => 11,
                    '∞' => 12,
                    _ => Math.Min(c - '0', '9' - 1)
                }];

                Vector2 halfRelative = info.Relative * new Vector2(1.0f, 0.5f);

                Vector3 tl = Vector3.Transform(new Vector3(pos.X - halfRelative.X, pos.Y + halfRelative.Y, pos.Z), billboard);
                Vector3 tr = Vector3.Transform(new Vector3(pos.X                 , pos.Y + halfRelative.Y, pos.Z), billboard);
                Vector3 bl = Vector3.Transform(new Vector3(pos.X - halfRelative.X, pos.Y - halfRelative.Y, pos.Z), billboard);
                Vector3 br = Vector3.Transform(new Vector3(pos.X                 , pos.Y - halfRelative.Y, pos.Z), billboard);

                AddUVTriangle(tl, tr, bl, new Vector2(info.UVMax, 0.0f), new Vector2(info.UVMin, 0.0f), new Vector2(info.UVMax, 1.0f), color);
                AddUVTriangle(tr, bl, br, new Vector2(info.UVMin, 0.0f), new Vector2(info.UVMax, 1.0f), new Vector2(info.UVMin, 1.0f), color);

                pos.X -= info.Relative.X + 0.2f;
            }
        }

        private bool ScreenRectDetection(Matrix4x4 matrix, Vector2 clientSize, Vector2 hit, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            Vector4 proj1 = Vector4.Transform(p1, matrix); ;
            Vector4 proj2 = Vector4.Transform(p2, matrix); ;
            Vector4 proj3 = Vector4.Transform(p3, matrix); ;
            Vector4 proj4 = Vector4.Transform(p4, matrix); ;

            Vector256<float> combined = Vector256.Create(proj1.X, proj1.Y, proj2.X, proj2.Y, proj3.X, proj3.Y, proj4.X, proj4.Y);

            combined /= Vector256.Create(proj1.W, proj1.W, proj2.W, proj2.W, proj3.W, proj3.W, proj4.W, proj4.W);
            combined *= Vector256.Create(clientSize.X, clientSize.Y, clientSize.X, clientSize.Y, clientSize.X, clientSize.Y, clientSize.X, clientSize.Y);

            Vector2 ab1 = new Vector2(combined.GetElement(0), combined.GetElement(1));
            Vector2 ab2 = new Vector2(combined.GetElement(2), combined.GetElement(3));
            Vector2 ab3 = new Vector2(combined.GetElement(4), combined.GetElement(5));
            Vector2 ab4 = new Vector2(combined.GetElement(6), combined.GetElement(7));

            //AddLine(new Vector3(ab1, 0.0f), new Vector3(ab2, 0.0f), 0xffffffff);
            //AddLine(new Vector3(ab2, 0.0f), new Vector3(ab3, 0.0f), 0xffffffff);
            //AddLine(new Vector3(ab3, 0.0f), new Vector3(ab4, 0.0f), 0xffffffff);
            //AddLine(new Vector3(ab4, 0.0f), new Vector3(ab1, 0.0f), 0xffffffff);

            return IsWithinTri(hit, ab1, ab2, ab4) || IsWithinTri(hit, ab2, ab3, ab4);
        }

        private static bool IsWithinTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNeg = Vector128.LessThanAny(Vector128.Create(d1, d2, d3, 0.0f), Vector128.CreateScalar(0.0f));
            bool hasPos = Vector128.GreaterThanAny(Vector128.Create(d1, d2, d3, 0.0f), Vector128.CreateScalar(0.0f));

            return !(hasNeg && hasPos);

            static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
            {
                Vector128<float> temp =
                    Vector128.Create(p1.X, p2.Y, p2.X, p1.Y) -
                    Vector128.Create(p3.X, p3.Y, p3.X, p3.Y);
                return temp.GetElement(0) * temp.GetElement(1) - temp.GetElement(2) * temp.GetElement(3);
            }
        }

        private const float SheetTotalWidth = 70.0f;
        private static readonly NumberSheetInfo[] s_numberSheetData = [
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 0.0f, 5 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(3.0f / 7.0f, 1.0f), 6 / SheetTotalWidth, 9 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 10 / SheetTotalWidth, 15 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 16 / SheetTotalWidth, 21 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 22 / SheetTotalWidth, 27 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 28 / SheetTotalWidth, 33 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 34 / SheetTotalWidth, 39 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 40 / SheetTotalWidth, 45 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 46 / SheetTotalWidth, 51 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 52 / SheetTotalWidth, 57 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(1.0f / 7.0f, 1.0f), 58 / SheetTotalWidth, 58 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(3.0f / 7.0f, 1.0f), 59 / SheetTotalWidth, 62 / SheetTotalWidth),
            new NumberSheetInfo(new Vector2(5.0f / 7.0f, 1.0f), 63 / SheetTotalWidth, 69 / SheetTotalWidth),
            ];

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly record struct Point(Vector3 Position, uint Color);
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly record struct UVPoint(Vector3 Position, Vector2 UV, uint Color);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly record struct Triangle(Point A, Point B, Point C);
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly record struct UVTriangle(UVPoint A, UVPoint B, UVPoint C);

        private readonly record struct NumberSheetInfo(Vector2 Relative, float UVMin, float UVMax);
    }
}
