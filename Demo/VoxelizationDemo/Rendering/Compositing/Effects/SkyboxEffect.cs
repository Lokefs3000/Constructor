using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;

namespace VoxelizationDemo.Rendering.Compositing.Effects
{
    public sealed class SkyboxEffect : RenderingEffect
    {
        private readonly Data _data;
        private readonly Pass _pass;

        private float _skyboxObjWeight;

        public SkyboxEffect()
        {
            _data = new Data();
            _pass = new Pass(_data);

            _skyboxObjWeight = 0.0f;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _pass.Dispose();
                }

                _disposedValue = true;
            }
        }

        protected internal override void Clear()
        {
            _data.Skybox = null;

            _skyboxObjWeight = 0.0f;
        }

        protected internal override void Blend(RenderingEffectData data, float weight)
        {
            Data selfData = (Data)data;

            if (weight > _skyboxObjWeight)
            {
                _data.Skybox = selfData.Skybox;
                _skyboxObjWeight = weight;
            }
        }

        protected internal override void FlushPasses(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            manager.PushRenderPass(_pass);
        }

        private sealed class Pass : IRenderPass, IDisposable
        {
            private readonly Data _data;

            private readonly ShaderAsset _skyboxShader;
            private readonly PropertyBlock _properties;

            private readonly MeshAsset _sphereMesh;

            public Pass(Data data)
            {
                _data = data;

                _skyboxShader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Effects/Skybox.shader");
                _properties = _skyboxShader.CreatePropertyBlock();

                _sphereMesh = AssetManager.LoadAsset<ModelAsset>("Engine/Models/Sphere.fbx")
                    .WaitIfNotLoaded()
                    .Meshes[0].Asset.Value;
            }

            public void Dispose()
            {
                _properties.Dispose();
            }

            public void SetupPasses(FrameGraphBuilder frameGraph)
            {
                if (!_skyboxShader.IsReady || !_sphereMesh.IsReady)
                    return;

                RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>();

                using (RasterPassBuilder builder = frameGraph.AddRasterPass("Skybox", out PassData passData))
                {
                    passData.Data = _data;
                    passData.Shader = _skyboxShader;
                    passData.Properties = _properties;
                    passData.Mesh = _sphereMesh;

                    passData.InDepth = cameraData.DepthTexture;
                    passData.OutColor = cameraData.ColorTexture;

                    builder.Read(passData.InDepth);
                    builder.Write(passData.OutColor);
                    builder.SetFunction(PassFunction);
                }
            }

            private static void PassFunction(RasterPassContext context)
            {
                RasterCommandBuffer cmd = context.CommandBuffer;
                PassData passData = context.GetPassData<PassData>()!;

                passData.Properties!.SetResource("txSky", passData.Data!.Skybox!);

                cmd.SetRenderTarget(passData.OutColor);
                cmd.SetDepthStencil(passData.InDepth);

                cmd.SetShader(passData.Shader!);
                cmd.SetProperties(passData.Properties!);

                IRenderMeshSource meshSource = ((IRawRenderMesh)passData.Mesh!).MeshSource!;
                cmd.SetVertexBuffer(meshSource.VertexBuffer);
                cmd.SetIndexBuffer(meshSource.IndexBuffer);

                cmd.DrawIndexedInstanced(passData.Mesh!.Args.VertexOrIndexCount, 1, 0, 0, 0);
            }

            private sealed class PassData : IPassData
            {
                public Data? Data;
                public ShaderAsset? Shader;
                public PropertyBlock? Properties;
                public MeshAsset? Mesh;

                public TextureHandle InDepth;
                public TextureHandle OutColor;

                void IPassData.Clear()
                {
                    Data = null;
                    Shader = null;
                    Properties = null;
                    Mesh = null;

                    InDepth = TextureHandle.Null;
                    OutColor = TextureHandle.Null;
                }
            }
        }

        public sealed class Data : RenderingEffectData
        {
            public TextureAsset? Skybox = null;
        }
    }
}
