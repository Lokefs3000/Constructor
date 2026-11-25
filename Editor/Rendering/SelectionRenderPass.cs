using Editor.Interaction;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Components;
using Primary.Scenes;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using RHI = Primary.RHI;

namespace Editor.Rendering
{
    internal sealed class SelectionRenderPass : IDisposable
    {
        private bool _disposedValue;

        private ShaderAsset? _writeDepth;
        private ShaderAsset? _sampleDepth;

        private ShaderBindGroup? _writeDepthBG;
        private ShaderBindGroup? _sampleDepthBG;

        private List<SceneEntity> _renderableEntities;
        private List<RenderObject> _meshes;

        private RHI.RenderTarget? _depthRenderTarget;
        private RHI.Buffer? _matrixBuffer;

        internal SelectionRenderPass()
        {
            _writeDepth = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/OutlineWriteDepth.hlsl");
            _sampleDepth = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/OutlineSampleDepth.hlsl");

            _renderableEntities = new List<SceneEntity>();
            _meshes = new List<RenderObject>();

            _depthRenderTarget = null;

            SelectionManager selection = Editor.GlobalSingleton.SelectionManager;

            selection.Selected += SelectedAdded;
            selection.Deselected += SelectedRemoved;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _matrixBuffer?.Dispose();
                    _depthRenderTarget?.Dispose();

                    SelectionManager selection = Editor.GlobalSingleton.SelectionManager;

                    selection.Selected -= SelectedAdded;
                    selection.Deselected -= SelectedRemoved;
                }

                _disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void SelectedAdded(SelectedBase @base)
        {
            if (@base is SelectedSceneEntity sceneEntity)
            {
                Debug.Assert(!_renderableEntities.Contains(sceneEntity.Entity));
                _renderableEntities.Add(sceneEntity.Entity);
            }
        }

        private void SelectedRemoved(SelectedBase @base)
        {
            if (@base is SelectedSceneEntity sceneEntity)
            {
                _renderableEntities.Remove(sceneEntity.Entity);
            }
        }

        public void SetupRenderState(RenderPass renderPass)
        {
            if (_renderableEntities.Count == 0 || _writeDepth?.Status != ResourceStatus.Success || _sampleDepth?.Status != ResourceStatus.Success)
                return;

            using (RasterPassDescription pass = renderPass.CreateRasterPass())
            {
                pass.SetThreadingPolicy(RenderPassThreadingPolicy.None);
                pass.SetDebugName("Selection");
                pass.SetFunction(PassFunction);
            }
        }

        private void PassFunction(RasterCommandBuffer commandBuffer, RenderPassData passData, object? userArg)
        {
            _meshes.Clear();
            for (int i = 0; i < _renderableEntities.Count; i++)
            {
                ref MeshRenderer mr = ref _renderableEntities[i].GetComponent<MeshRenderer>();
                if (!Unsafe.IsNullRef(ref mr) && mr.Mesh != null)
                {
                    _meshes.Add(new RenderObject(mr.Mesh, _renderableEntities[i]));
                }
            }

            if (_meshes.Count > 0)
            {
                RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;

                using (new CommandBufferEventScope(commandBuffer, "Selection"))
                {
                    PrepareRenderBuffers(commandBuffer, viewportData);
                    RenderObjectsToDepth(commandBuffer, viewportData);
                    SampleDepthForOutline(commandBuffer, viewportData);
                }
            }
        }

        private void PrepareRenderBuffers(RasterCommandBuffer commandBuffer, RenderPassViewportData viewportData)
        {
            if (_matrixBuffer == null)
            {
                _matrixBuffer = RenderingManager.Device.CreateBuffer(new RHI.BufferDescription
                {
                    ByteWidth = (uint)Unsafe.SizeOf<Matrix4x4>(),
                    Stride = (uint)Unsafe.SizeOf<Matrix4x4>(),
                    CpuAccessFlags = RHI.CPUAccessFlags.Write,
                    Memory = RHI.MemoryUsage.Dynamic,
                    Mode = RHI.BufferMode.None,
                    Usage = RHI.BufferUsage.ConstantBuffer
                }, nint.Zero);
            }

            if (_writeDepthBG == null)
            {
                _writeDepthBG = _writeDepth!.CreateDefaultBindGroup();
                _writeDepthBG.SetResource("cbObject", _matrixBuffer);
            }

            if (_sampleDepthBG == null)
            {
                _sampleDepthBG = _sampleDepth!.CreateDefaultBindGroup();
            }

            if (_depthRenderTarget == null || _depthRenderTarget.Description.Dimensions != viewportData.BackBufferRenderTarget.Description.Dimensions)
            {
                _depthRenderTarget?.Dispose();
                _depthRenderTarget = RenderingManager.Device.CreateRenderTarget(new RHI.RenderTargetDescription
                {
                    ColorFormat = RHI.RenderTargetFormat.Undefined,
                    DepthFormat = RHI.DepthStencilFormat.R24tX8ui,

                    Dimensions = viewportData.BackBufferRenderTarget.Description.Dimensions,
                    ShaderVisibility = RHI.RenderTargetVisiblity.Stencil
                });

                _sampleDepthBG.SetResource("txStencil", _depthRenderTarget.StencilTexture);
            }
        }

        private void RenderObjectsToDepth(RasterCommandBuffer commandBuffer, RenderPassViewportData viewportData)
        {
            uint width = (uint)viewportData.CameraRenderTarget.Description.Dimensions.Width;
            uint height = (uint)viewportData.CameraRenderTarget.Description.Dimensions.Height;

            //commandBuffer.CopyTextureRegion(
            //    viewportData.CameraRenderTarget.DepthTexture!,
            //    new RHI.TextureLocation(0, 0, 0, width, height, 1),
            //    0,
            //    _depthRenderTarget!.DepthTexture!,
            //    new RHI.TextureLocation(0, 0, 0, width, height, 1),
            //    0);
            commandBuffer.ClearDepthStencil(_depthRenderTarget!, RHI.ClearFlags.Depth | RHI.ClearFlags.Stencil, stencil: 0x00);

            commandBuffer.SetDepthStencil(_depthRenderTarget!);
            commandBuffer.SetViewport(new RHI.Viewport(0, 0, _depthRenderTarget!.Description.Dimensions.Width, _depthRenderTarget.Description.Dimensions.Height));
            commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 100000, 10000));

            commandBuffer.SetShader(_writeDepth!);
            commandBuffer.SetBindGroups(_writeDepthBG!);
            commandBuffer.CommitShaderResources();

            commandBuffer.SetConstants(0.0f/*1.0f / (viewportData.Camera.FarClip / viewportData.Camera.NearClip)*/);
            commandBuffer.SetStencilReference(255);

            for (int i = 0; i < _meshes.Count; i++)
            {
                RenderObject ro = _meshes[i];

                Matrix4x4 model = Matrix4x4.Identity;

                ref WorldTransform transform = ref ro.Entity.GetComponent<WorldTransform>();
                if (!Unsafe.IsNullRef(ref transform))
                {
                    model = transform.Transformation;
                }

                {
                    Span<Matrix4x4> span = commandBuffer.Map<Matrix4x4>(_matrixBuffer!, RHI.MapIntent.Write);
                    span[0] = model * viewportData.VP;
                    commandBuffer.Unmap(_matrixBuffer!);
                }

                IRenderMeshSource? meshSource = ro.Mesh.Source;
                if (meshSource.VertexBuffer == null || meshSource.IndexBuffer == null)
                    continue;

                commandBuffer.SetVertexBuffer(0, meshSource.VertexBuffer);
                commandBuffer.SetIndexBuffer(meshSource.IndexBuffer);
                commandBuffer.DrawIndexedInstanced(new RHI.DrawIndexedInstancedArgs(ro.Mesh.IndexCount, ro.Mesh.IndexOffset, (int)ro.Mesh.VertexOffset));
            }
        }

        private void SampleDepthForOutline(RasterCommandBuffer commandBuffer, RenderPassViewportData viewportData)
        {
            commandBuffer.SetRenderTarget(viewportData.BackBufferRenderTarget);

            commandBuffer.SetShader(_sampleDepth!);
            commandBuffer.SetBindGroups(_sampleDepthBG!);
            commandBuffer.CommitShaderResources();

            commandBuffer.SetConstants(new SampleConstants(viewportData.Camera.NearClip, viewportData.Camera.FarClip));

            commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs(3));
        }

        private readonly record struct RenderObject(RawRenderMesh Mesh, SceneEntity Entity);

        private readonly record struct SampleConstants(float Near, float Far);
    }
}
