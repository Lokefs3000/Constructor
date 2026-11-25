using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Primary.Assets;
using Primary.Collections;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Rendering;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Diagnostics;
using Primary.Threading;
using VoxelizationDemo.Rendering.Compositing;
using VoxelizationDemo.Rendering.Compositing.Effects;
using VoxelizationDemo.Rendering.Diagnostics;
using VoxelizationDemo.Rendering.Passes;

namespace VoxelizationDemo.Rendering
{
    internal sealed class WorldRenderStage : IRenderStage
    {
        private RenderList? _renderList;
        private LightCollector? _lightCollector;
        private CompositingManager? _compositingManager;

        private SetupDataPass? _setupDataPass;
        private ClusterLightsPass? _clusterLightsPass;
        private WorldOpaquePass? _worldOpaquePass;

        public void Install(RenderingManager manager)
        {
            _renderList = manager.BatchingManager.CreateRenderList();
            // _renderList.DefaultMaterial = AssetManager.LoadAsset<MaterialAsset>("Content/Materials/Missing.mat");

            _lightCollector = new LightCollector();
            _compositingManager = new CompositingManager();

            _setupDataPass = new SetupDataPass();
            _clusterLightsPass = new ClusterLightsPass();
            _worldOpaquePass = new WorldOpaquePass();
        }

        public void Uninstall(RenderingManager manager)
        {
            _renderList?.Dispose();
            _renderList = null;

            _lightCollector = null;
            _compositingManager = null;

            _setupDataPass = null;
            _clusterLightsPass = null;
            _worldOpaquePass = null;
        }

        public void FlushPassesForCamera(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            RenderCameraData cameraData = context.Get<RenderCameraData>();

            JobHandle batchWorldJob = manager.RequestBatch(_renderList!, new BatchingOptions
            {
                Origin = cameraData.Transform.Position,
                MaxViewingDistance = cameraData.ZFar,

                ViewFrustrum = cameraData.ViewFrustrum,
            }, JobHandle.Completed);

            RentedList<JobHandle> jobHandleList = [batchWorldJob];
            _lightCollector!.CollectLights(cameraData.ViewFrustrum, cameraData.Transform.Transformation.Translation, ref jobHandleList);

            _compositingManager!.UpdateEffects(cameraData.Transform.Position);

            JobScheduler.CombineAll(jobHandleList.AsSpan()).WaitForCompletion();
            if (!_renderList!.IsEmpty)
            {
                RenderStageBlackboard generalBlackboard = blackboard.Add<RenderStageBlackboard>();
                generalBlackboard.RenderList = _renderList;

                // foreach (PointLightData light in _lightCollector.PointLights)
                // {
                //     Gizmos.DrawWireSphere(light.Position, 0.1f, Color.Yellow);
                //     Gizmos.DrawWireSphere(light.Position, light.Radius, new Color(light.Diffuse.WithElement(3, 1.0f)));
                // }

                generalBlackboard.LightCollector = _lightCollector;
            }
            else
            {
                blackboard.Add<RenderStageBlackboard>();
            }

            manager.PushRenderPass(_setupDataPass!);
            manager.PushRenderPass(_clusterLightsPass!);
            manager.PushRenderPass(_worldOpaquePass!);
            _compositingManager.TryFlushActiveEffect<SkyboxEffect.Data>(manager, blackboard, context);
            _compositingManager.TryFlushActiveEffect<TonemappingEffect.Data>(manager, blackboard, context);

            //OctreeVisualizer.Visualize(manager.PartitioningManager, cameraData.ViewFrustrum, cameraData.Transform.Position);
            jobHandleList.Dispose();
        }

        public void FlushPassesForWindow(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
        }

        public RenderList? RenderList => _renderList;
        public LightCollector? LightCollector => _lightCollector;
        public CompositingManager CompositingManager => _compositingManager!;
    }
}
