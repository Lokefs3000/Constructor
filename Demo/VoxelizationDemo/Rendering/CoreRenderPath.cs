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
    internal sealed class CoreRenderPath : IRenderPath
    {
        private RenderList? _renderList;
        private LightCollector? _lightCollector;

        public void Install(RenderingManager manager)
        {
            _renderList = manager.BatchingManager.CreateRenderList();
            _renderList.DefaultMaterial = AssetManager.LoadAsset<MaterialAsset>("Content/Materials/Missing.mat");

            _lightCollector = new LightCollector();

            manager.RenderPassManager.AddRenderPass<SetupDataPass>();
            manager.RenderPassManager.AddRenderPass<ClusterLightsPass>();
            manager.RenderPassManager.AddRenderPass<WorldOpaquePass>();

            // manager.RenderPassManager.AddRenderPass<LightHeatmapPass>();
            manager.RenderPassManager.AddRenderPass<ImGuiRenderPass>();
        }

        public void Uninstall(RenderingManager manager)
        {
            _renderList?.Dispose();
            _renderList = null;

            _lightCollector = null;

            manager.RenderPassManager.RemoveRenderPass<SetupDataPass>();
            manager.RenderPassManager.RemoveRenderPass<ClusterLightsPass>();
            manager.RenderPassManager.RemoveRenderPass<WorldOpaquePass>();

            // manager.RenderPassManager.RemoveRenderPass<LightHeatmapPass>();
            manager.RenderPassManager.RemoveRenderPass<ImGuiRenderPass>();
        }

        public void PreRenderPassSetup(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            RenderCameraData cameraData = context.Get<RenderCameraData>()!;
            manager.BatchingManager.BatchWorld(_renderList!, new BatchWorldSetup(manager.OctreeManager, cameraData.ViewFrustrum));

            RenderPathBlackboard generalBlackboard = blackboard.Add<RenderPathBlackboard>();
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

                // foreach (PointLightData light in _lightCollector.PointLights)
                // {
                //     Gizmos.DrawWireSphere(light.Position, 0.1f, Color.Yellow);
                //     Gizmos.DrawWireSphere(light.Position, light.Radius, new Color(light.Diffuse.WithElement(3, 1.0f)));
                // }

                generalBlackboard.LightCollector = _lightCollector;
            }

            // Draw debug stuff
            // OctreeVisualizer.Visualize(manager.OctreeManager, cameraData.ViewFrustrum);
        }

        public RenderList? RenderList => _renderList;
        public LightCollector? LightCollector => _lightCollector;
    }
}
