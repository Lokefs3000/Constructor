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
using VoxelizationDemo.Rendering.Diagnostics;
using VoxelizationDemo.Rendering.Passes;

namespace VoxelizationDemo.Rendering
{
    internal sealed class WorldRenderStage : IRenderStage
    {
        private RenderList? _renderList;
        private LightCollector? _lightCollector;

        private SetupDataPass? _setupDataPass;
        private ClusterLightsPass? _clusterLightsPass;
        private WorldOpaquePass? _worldOpaquePass;

        public void Install(RenderingManager manager)
        {
            _renderList = manager.BatchingManager.CreateRenderList();
            _renderList.DefaultMaterial = AssetManager.LoadAsset<MaterialAsset>("Content/Materials/Missing.mat");

            _lightCollector = new LightCollector();

            _setupDataPass = new SetupDataPass();
            _clusterLightsPass = new ClusterLightsPass();
            _worldOpaquePass = new WorldOpaquePass();
        }

        public void Uninstall(RenderingManager manager)
        {
            _renderList?.Dispose();
            _renderList = null;

            _lightCollector = null;

            _setupDataPass = null;
            _clusterLightsPass = null;
            _worldOpaquePass = null;
        }

        public void FlushPassesForCamera(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            RenderCameraData cameraData = context.Get<RenderCameraData>();

            manager.RequestBatch(_renderList!, new BatchingOptions
            {
                ViewFrustrum = cameraData.ViewFrustrum,
                Center = cameraData.Transform.Position
            });

            RenderStageBlackboard generalBlackboard = blackboard.Add<RenderStageBlackboard>();
            generalBlackboard.RenderList = _renderList!.IsEmpty ? null : _renderList;

            if (generalBlackboard.RenderList != null)
            {
                RentedList<JobHandle> jobHandleList = new RentedList<JobHandle>();
                _lightCollector!.CollectLights(cameraData.ViewFrustrum, cameraData.Transform.Transformation.Translation, ref jobHandleList);

                if (jobHandleList.Count > 0)
                {
                    JobHandle monolith = JobScheduler.CombineAll(jobHandleList.AsSpan());
                    JobScheduler.Flush(jobHandleList.AsSpan());

                    monolith.WaitForCompletion();
                }

                jobHandleList.Dispose();

                foreach (PointLightData light in _lightCollector.PointLights)
                {
                    Gizmos.DrawWireSphere(light.Position, 0.1f, Color.Yellow);
                    Gizmos.DrawWireSphere(light.Position, light.Radius, new Color(light.Diffuse.WithElement(3, 1.0f)));
                }

                generalBlackboard.LightCollector = _lightCollector;
            }

            manager.PushRenderPass(_setupDataPass!);
            manager.PushRenderPass(_clusterLightsPass!);
            manager.PushRenderPass(_worldOpaquePass!);
        }

        public void FlushPassesForWindow(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
        }

        public RenderList? RenderList => _renderList;
        public LightCollector? LightCollector => _lightCollector;
    }
}
