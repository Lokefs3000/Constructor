using Arch.LowLevel;
using Editor.DearImGui;
using Editor.GeoEdit;
using Editor.Geometry;
using Editor.Geometry.Shapes;
using Editor.Interaction;
using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Pass;
using Primary.Rendering.Raw;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using RHI = Primary.RHI;

namespace Editor.Rendering
{
    internal sealed class GeoToolRenderPass : IDisposable
    {
        private ShaderAsset _defLineShader;
        private ShaderAsset _defTriangleShader;
        private ShaderAsset _billboardShader;

        private ShaderBindGroup _defLineBG;
        private ShaderBindGroup _billboardBG;

        private UnsafeList<sbBillboardData> _billboards;
        private UnsafeList<LineGeometry> _lineGeometry;
        private UnsafeList<TriangleGeometry> _triangleGeometry;

        private RHI.Buffer _cbVertex;

        private RHI.Buffer? _billboardBuffer;
        private int _billboardBufferSize;

        private RHI.Buffer? _geometryBuffer;
        private int _geometryBufferSize;

        private bool _disposedValue;

        internal GeoToolRenderPass()
        {
            _defLineShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/ToolsLine.hlsl", true);
            _defTriangleShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/GeoTriangle.hlsl", true);
            _billboardShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/GeoBillboard.hlsl", true);

            _defLineBG = _defLineShader.CreateDefaultBindGroup();
            _billboardBG = _billboardShader.CreateDefaultBindGroup();

            _billboards = new UnsafeList<sbBillboardData>(8);
            _lineGeometry = new UnsafeList<LineGeometry>(8);
            _triangleGeometry = new UnsafeList<TriangleGeometry>(8);

            _cbVertex = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<cbWorldData>(),
                Stride = (uint)Unsafe.SizeOf<cbWorldData>(),
                CpuAccessFlags = RHI.CPUAccessFlags.Write,
                Memory = RHI.MemoryUsage.Dynamic,
                Mode = RHI.BufferMode.None,
                Usage = RHI.BufferUsage.ConstantBuffer
            }, nint.Zero);

            //_defLineBG.SetResource("cbVertex", _cbVertex);
            _billboardBG.SetResource("cbVertex", _cbVertex);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _cbVertex.Dispose();
                    _billboardBuffer?.Dispose();
                }

                _billboards.Dispose();
                _lineGeometry.Dispose();
                _triangleGeometry.Dispose();

                _disposedValue = true;
            }
        }

        ~GeoToolRenderPass()
        {
            Dispose(disposing: false);
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

        private void PassFunction(RasterCommandBuffer commandBuffer, RenderPassData passData, object? userArg)
        {
            SceneView view = Editor.GlobalSingleton.SceneView;
            GeoEditorView editor = Editor.GlobalSingleton.GeoEditorView;

            _billboards.Clear();
            _lineGeometry.Clear();
            _triangleGeometry.Clear();

            if (editor.ActiveScene != null)
            {
                SelectedBase? active = SelectionManager.ActiveContext;
                foreach (SelectedBase @base in SelectionManager.ActiveSelection)
                {
                    if (@base is SelectedGeoBrush brush)
                    {
                        if (brush.Brush != null && brush.Brush.Shape != null)
                        {
                            DrawGeoBrushLineShape(brush.Brush.Transform, editor.ActiveScene.VertexCache, brush.Brush.Shape);
                        }
                    }
                }

                GeoToolRenderInterface @interface = new GeoToolRenderInterface(ref _billboards, ref _lineGeometry, ref _triangleGeometry);

                IGeoTool? tool = editor.CurrentTool;
                tool?.Render(in @interface);

                if (editor.LastPickResult.HasValue)
                {
                    GeoPickResult result = editor.LastPickResult.Value;
                    AddLine(result.Position, result.Position + result.Normal, 0xffff0000);
                }

                RenderNewGeometry(commandBuffer, passData);
            }
        }

        private void RenderNewGeometry(RasterCommandBuffer commandBuffer, RenderPassData passData)
        {
            RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;

            commandBuffer.SetRenderTarget(viewportData.BackBufferRenderTarget);
            commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 10000, 10000));

            SceneView view = Editor.GlobalSingleton.SceneView;

            {
                Span<cbWorldData> mapped = commandBuffer.Map<cbWorldData>(_cbVertex, RHI.MapIntent.Write);
                mapped[0] = new cbWorldData { View = view.ViewMatrix, Projection = view.ProjectionMatrix };
                commandBuffer.Unmap(_cbVertex);
            }

            int totalPointsInGeometry = _triangleGeometry.Count * 3 + _lineGeometry.Count * 2;
            if (_geometryBufferSize < totalPointsInGeometry)
            {
                _geometryBuffer?.Dispose();
                _geometryBufferSize = (int)(totalPointsInGeometry * 1.5f);
                _geometryBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                {
                    ByteWidth = (uint)(_geometryBufferSize * Unsafe.SizeOf<PointGeometry>()),
                    Stride = (uint)Unsafe.SizeOf<PointGeometry>(),
                    CpuAccessFlags = RHI.CPUAccessFlags.Write,
                    Memory = RHI.MemoryUsage.Dynamic,
                    Mode = RHI.BufferMode.None,
                    Usage = RHI.BufferUsage.VertexBuffer
                }, nint.Zero);
            }

            if (totalPointsInGeometry > 0)
            {
                Span<PointGeometry> mapped = commandBuffer.Map<PointGeometry>(_geometryBuffer!, RHI.MapIntent.Write, totalPointsInGeometry);

                if (_triangleGeometry.Count > 0)
                {
                    MemoryMarshal.Cast<TriangleGeometry, PointGeometry>(_triangleGeometry.AsSpan()).CopyTo(mapped);
                    mapped = mapped.Slice(_triangleGeometry.Count * 3);
                }

                if (_lineGeometry.Count > 0)
                {
                    MemoryMarshal.Cast<LineGeometry, PointGeometry>(_lineGeometry.AsSpan()).CopyTo(mapped);
                }

                commandBuffer.Unmap(_geometryBuffer!);
            }

            if (_billboards.Count > 0)
            {
                if (_billboardBuffer == null || _billboardBufferSize < _billboards.Count)
                {
                    _billboardBuffer?.Dispose();
                    _billboardBufferSize = (int)(_billboards.Count * 1.5f);
                    _billboardBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                    {
                        ByteWidth = (uint)(Unsafe.SizeOf<sbBillboardData>() * _billboardBufferSize),
                        Stride = (uint)Unsafe.SizeOf<sbBillboardData>(),
                        CpuAccessFlags = RHI.CPUAccessFlags.Write,
                        Memory = RHI.MemoryUsage.Dynamic,
                        Mode = RHI.BufferMode.Structured,
                        Usage = RHI.BufferUsage.ShaderResource
                    }, nint.Zero);
                }

                {
                    Span<sbBillboardData> data = commandBuffer.Map<sbBillboardData>(_billboardBuffer, RHI.MapIntent.Write, _billboards.Count);
                    _billboards.AsSpan().CopyTo(data);
                    commandBuffer.Unmap(_billboardBuffer);
                }

                _billboardBG.SetResource("sbBillboards", _billboardBuffer);

                commandBuffer.SetShader(_billboardShader);
                commandBuffer.SetBindGroups(_billboardBG);
                commandBuffer.CommitShaderResources();

                commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs(6, instanceCount: (uint)_billboards.Count));
            }

            if (_triangleGeometry.Count > 0)
            {
                commandBuffer.SetShader(_defTriangleShader);
                commandBuffer.SetConstants(viewportData.VP);
                commandBuffer.CommitShaderResources();

                commandBuffer.SetVertexBuffer(0, _geometryBuffer!);

                commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs((uint)(_triangleGeometry.Count * 3)));
            }

            if (_lineGeometry.Count > 0)
            {
                commandBuffer.SetShader(_defLineShader);
                commandBuffer.SetConstants(viewportData.VP);
                commandBuffer.CommitShaderResources();

                commandBuffer.SetVertexBuffer(0, _geometryBuffer!);

                commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs((uint)(_lineGeometry.Count * 2), (uint)(_triangleGeometry.Count * 3)));
            }
        }

        private void DrawGeoBrushLineShape(GeoTransform transform, GeoVertexCache? vertexCache, IGeoShape shape)
        {
            CachedGeoShape cachedShape;
            if (vertexCache == null || !vertexCache.Retrieve(shape, out cachedShape))
            {
                cachedShape = GeoGenerator.Transform(shape.GenerateMesh(), transform);
                vertexCache?.Store(shape, cachedShape);
            }

            if (shape is GeoBoxShape boxShape)
            {
                AddLine(cachedShape.Vertices[0].Position, cachedShape.Vertices[1].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[2].Position, cachedShape.Vertices[3].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[4].Position, cachedShape.Vertices[5].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[6].Position, cachedShape.Vertices[7].Position, 0xffffffff);

                AddLine(cachedShape.Vertices[0].Position, cachedShape.Vertices[5].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[1].Position, cachedShape.Vertices[4].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[2].Position, cachedShape.Vertices[7].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[3].Position, cachedShape.Vertices[6].Position, 0xffffffff);

                AddLine(cachedShape.Vertices[0].Position, cachedShape.Vertices[2].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[1].Position, cachedShape.Vertices[3].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[4].Position, cachedShape.Vertices[6].Position, 0xffffffff);
                AddLine(cachedShape.Vertices[5].Position, cachedShape.Vertices[7].Position, 0xffffffff);
            }
        }

        private void AddBillboard(Vector3 position, float size, Vector3 color)
        {
            _billboards.Add(new sbBillboardData
            {
                Position = position,
                Size = size,
                Color = color
            });
        }

        private void AddLine(Vector3 from, Vector3 to, uint color)
        {
            _lineGeometry.Add(new LineGeometry
            {
                Start = new PointGeometry(from, color),
                End = new PointGeometry(to, color)
            });
        }

        private struct cbWorldData
        {
            public Matrix4x4 Projection;
            public Matrix4x4 View;
        }

        internal struct sbBillboardData
        {
            public Vector3 Position;
            public float Size;
            public Vector3 Color;
        }

        internal record struct PointGeometry(Vector3 Position, uint Color);
        internal record struct LineGeometry(PointGeometry Start, PointGeometry End);
        internal record struct TriangleGeometry(PointGeometry A, PointGeometry B, PointGeometry C);
    }

    internal ref struct GeoToolRenderInterface
    {
        private ref UnsafeList<GeoToolRenderPass.sbBillboardData> _billboards;
        private ref UnsafeList<GeoToolRenderPass.LineGeometry> _lines;
        private ref UnsafeList<GeoToolRenderPass.TriangleGeometry> _triangles;

        internal GeoToolRenderInterface(ref UnsafeList<GeoToolRenderPass.sbBillboardData> billboards, ref UnsafeList<GeoToolRenderPass.LineGeometry> lines, ref UnsafeList<GeoToolRenderPass.TriangleGeometry> triangles)
        {
            _billboards = ref billboards;
            _lines = ref lines;
            _triangles = ref triangles;
        }

        internal void AddBillboard(Vector3 position, float size, Vector3 color)
        {
            _billboards.Add(new GeoToolRenderPass.sbBillboardData
            {
                Position = position,
                Size = size,
                Color = color
            });
        }

        internal void AddLine(Vector3 from, Vector3 to, uint color)
        {
            _lines.Add(new GeoToolRenderPass.LineGeometry
            {
                Start = new GeoToolRenderPass.PointGeometry(from, color),
                End = new GeoToolRenderPass.PointGeometry(to, color)
            });
        }

        internal void AddTriangle(Vector3 a, Vector3 b, Vector3 c, uint color)
        {
            _triangles.Add(new GeoToolRenderPass.TriangleGeometry
            {
                A = new GeoToolRenderPass.PointGeometry(a, color),
                B = new GeoToolRenderPass.PointGeometry(b, color),
                C = new GeoToolRenderPass.PointGeometry(c, color),
            });
        }
    }
}
