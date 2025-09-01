using Arch.Core;
using Primary.Assets;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering.Batching;
using System.Diagnostics;
using System.Numerics;

namespace Primary.Rendering
{
    public sealed class FrameCollector
    {
        internal FrameCollector()
        {

        }

        internal void SetupScene(RenderScene renderScene)
        {
            using (new ProfilingScope("SetupScene"))
            {
                renderScene.ClearInternalData();

                World world = Engine.GlobalSingleton.SceneManager.World;

                //TODO: fix shit naming scheme
                CollectCameras job = new CollectCameras { Scene = renderScene };

                world.InlineEntityQuery<CollectCameras, WorldTransform, CameraProjectionData>(CollectCameras.Description, ref job);
            }
        }

        internal void CollectWorld(RenderBatcher batcher)
        {
            using (new ProfilingScope("CollectWorld"))
            {
                batcher.ClearBatchData();

                World world = Engine.GlobalSingleton.SceneManager.World;

                CollectMeshRenderers job = new CollectMeshRenderers { Batcher = batcher };
                world.InlineEntityQuery<CollectMeshRenderers, WorldTransform, MeshRenderer>(CollectMeshRenderers.Description, ref job);
            }
        }

        private record struct CollectMeshRenderers : IForEachWithEntity<WorldTransform, MeshRenderer>
        {
            public static QueryDescription Description =
                new QueryDescription().WithAll<WorldTransform, MeshRenderer>();

            public RenderBatcher Batcher;

            public void Update(Entity entity, ref WorldTransform world, ref MeshRenderer renderer)
            {
                if (renderer.Mesh != null)
                {
                    RenderMesh mesh = renderer.Mesh;
                    //TODO: add backup material
                    MaterialAsset material = renderer.Material!;

                    Debug.Assert(material.Shader != null);
                    FlagRenderBatch flagBatcher = Batcher.GetFlagBatch(material.Shader!);

                    uint materialIdx = Batcher.GetMaterialIndex(material);
                    flagBatcher.AddFlag(mesh, materialIdx, ref world.Transformation);
                }
            }
        }

        private record struct CollectCameras : IForEachWithEntity<WorldTransform, CameraProjectionData>
        {
            public static QueryDescription Description =
                new QueryDescription().WithAll<WorldTransform, CameraProjectionData>();

            public RenderScene Scene;

            public void Update(Entity entity, ref WorldTransform transform, ref CameraProjectionData projectionData)
            {
                Scene.AddOutputViewport(new RSOutputViewport
                {
                    Id = (long)entity.Id | ((long)entity.WorldId << 32),

                    Target = null,
                    ClientSize = projectionData.ClientSize,
                    ViewMatrix = projectionData.ViewMatrix,
                    ProjectionMatrix = projectionData.ProjectionMatrix,

                    ViewPosition = transform.Transformation.Translation,
                    ViewDirection = Vector3.Normalize(new Vector3(transform.Transformation.M31, transform.Transformation.M32, transform.Transformation.M33)),

                    RootEntity = entity,
                });
            }
        }
    }
}
