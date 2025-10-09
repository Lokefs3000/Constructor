using CommunityToolkit.HighPerformance;
using Editor.Interaction;
using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Pass;
using Primary.Rendering.Raw;
using System.Numerics;
using System.Runtime.CompilerServices;
using RHI = Primary.RHI;

namespace Editor.Rendering
{
    internal sealed class GizmoRenderPass : IDisposable
    {
        private ShaderAsset _gridShader;
        private ShaderAsset _lineShader;
        private ShaderAsset _sphereShader;

        private ShaderBindGroup _sphereShaderBG;

        private RHI.Buffer? _lineVertexBuffer;
        private int _lineVertexBufferSize;

        private RHI.Buffer? _sphereBuffer;
        private int _sphereBufferSize;

        private RHI.Buffer? _cubeBuffer;
        private int _cubeBufferSize;

        private RHI.Buffer _globalBuffer;

        private bool _disposedValue;

        internal GizmoRenderPass()
        {
            _gridShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/SceneGrid.hlsl", true);
            _lineShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmos/GZLineShader.hlsl", true);
            _sphereShader = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Gizmos/GZSphereShader.hlsl", true);

            _sphereShaderBG = _sphereShader.CreateDefaultBindGroup();

            _globalBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
            {
                ByteWidth = (uint)Unsafe.SizeOf<cbGlobalData>(),
                Stride = (uint)Unsafe.SizeOf<cbGlobalData>(),
                Memory = RHI.MemoryUsage.Dynamic,
                Usage = RHI.BufferUsage.ConstantBuffer,
                Mode = RHI.BufferMode.None,
                CpuAccessFlags = RHI.CPUAccessFlags.Write
            }, nint.Zero);

            _sphereShaderBG.SetResource("cbGlobal", _globalBuffer);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _globalBuffer.Dispose();

                    _lineVertexBuffer?.Dispose();
                    _sphereBuffer?.Dispose();
                    _cubeBuffer?.Dispose();
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
            DrawGrid(commandBuffer, passData);
            DrawGizmos(commandBuffer, passData);
        }

        private void DrawGrid(RasterCommandBuffer commandBuffer, RenderPassData passData)
        {
            RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;

            commandBuffer.SetRenderTarget(viewportData.BackBufferRenderTarget);
            commandBuffer.SetDepthStencil(viewportData.CameraRenderTarget);
            commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 10000, 10000));

            commandBuffer.SetShader(_gridShader);
            commandBuffer.SetConstants(new cbWorldData { VP = viewportData.VP, GridScale = 2.0f / ToolManager.SnapScale, GridDistance = 300.0f });
            commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs(6));
        }

        private void DrawGizmos(RasterCommandBuffer commandBuffer, RenderPassData passData)
        {
            Gizmos gizmos = Gizmos.Instance;

            if (gizmos.IsEmpty)
                return;

            Span<GZLineVertex> lineVertices = gizmos.Vertices;
            Span<GZSphereData> sphereDatas = gizmos.Spheres;
            Span<GZCubeData> cubeDatas = gizmos.Cubes;

            {
                if (!lineVertices.IsEmpty)
                {
                    if (_lineVertexBuffer == null || _lineVertexBufferSize < lineVertices.Length)
                    {
                        _lineVertexBuffer?.Dispose();
                        _lineVertexBufferSize = lineVertices.Length * 2;
                        _lineVertexBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                        {
                            ByteWidth = (uint)(Unsafe.SizeOf<GZLineVertex>() * _lineVertexBufferSize),
                            Stride = (uint)Unsafe.SizeOf<GZLineVertex>(),
                            Memory = RHI.MemoryUsage.Dynamic,
                            Usage = RHI.BufferUsage.VertexBuffer,
                            Mode = RHI.BufferMode.None,
                            CpuAccessFlags = RHI.CPUAccessFlags.Write
                        }, nint.Zero);
                    }

                    {
                        lineVertices.CopyTo(commandBuffer.Map<GZLineVertex>(_lineVertexBuffer, RHI.MapIntent.Write, lineVertices.Length));
                        commandBuffer.Unmap(_lineVertexBuffer);
                    }
                }

                if (!sphereDatas.IsEmpty)
                {
                    if (_sphereBuffer == null || _sphereBufferSize < sphereDatas.Length)
                    {
                        _sphereBuffer?.Dispose();
                        _sphereBufferSize = sphereDatas.Length * 2;
                        _sphereBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                        {
                            ByteWidth = (uint)(Unsafe.SizeOf<GZSphereData>() * _sphereBufferSize),
                            Stride = (uint)Unsafe.SizeOf<GZSphereData>(),
                            Memory = RHI.MemoryUsage.Dynamic,
                            Usage = RHI.BufferUsage.ShaderResource,
                            Mode = RHI.BufferMode.Structured,
                            CpuAccessFlags = RHI.CPUAccessFlags.Write
                        }, nint.Zero);

                        _sphereShaderBG.SetResource("sbSpheres", _sphereBuffer);
                    }

                    {
                        sphereDatas.CopyTo(commandBuffer.Map<GZSphereData>(_sphereBuffer, RHI.MapIntent.Write, sphereDatas.Length));
                        commandBuffer.Unmap(_sphereBuffer);
                    }
                }
            }

            RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;

            {
                Span<cbGlobalData> globalData = commandBuffer.Map<cbGlobalData>(_globalBuffer, RHI.MapIntent.Write);
                globalData[0] = new cbGlobalData { View = viewportData.View, Projection = viewportData.Projection };
                commandBuffer.Unmap(_globalBuffer);
            }

            Span<GizmoDrawCommand> drawCommands = gizmos.DrawCommands;
            int iLarge = drawCommands.Length - 1;

            int lineOffset = 0;
            int sphereOffset = 0;

            for (int i = 0; i < drawCommands.Length; i++)
            {
                ref GizmoDrawCommand command = ref drawCommands[i];
                if (command.ElementCount == 0)
                    continue;

                switch (command.PolyType)
                {
                    case GizmoPolyType.Line:
                        {
                            commandBuffer.SetShader(_lineShader);
                            commandBuffer.SetConstants(viewportData.VP);
                            commandBuffer.ClearResources();

                            commandBuffer.SetVertexBuffer(0, _lineVertexBuffer!);
                            commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs((uint)command.ElementCount, (uint)lineOffset));

                            lineOffset += command.ElementCount;
                            break;
                        }
                    case GizmoPolyType.Cube:
                        {
                            throw new NotImplementedException();
                        }
                    case GizmoPolyType.Sphere:
                        {
                            commandBuffer.SetShader(_sphereShader);
                            commandBuffer.ClearConstants();
                            commandBuffer.SetBindGroups(_sphereShaderBG);
                            commandBuffer.CommitShaderResources();

                            commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs(6, 0, (uint)command.ElementCount, (uint)sphereOffset));

                            sphereOffset += command.ElementCount;
                            break;
                        }
                }
            }
        }

        private struct cbWorldData
        {
            public Matrix4x4 VP;
            public float GridScale;
            public float GridDistance;
        }

        private struct cbGlobalData
        {
            public Matrix4x4 View;
            public Matrix4x4 Projection;
        }
    }
}
