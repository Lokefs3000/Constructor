using Arch.Core;
using CommunityToolkit.HighPerformance;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Scenes;

namespace Primary.Rendering2
{
    public sealed class RenderWorld
    {
        private List<RenderOutputData> _outputs;

        internal RenderWorld()
        {
            _outputs = new List<RenderOutputData>();
        }

        internal void SetupWorld()
        {
            using (new ProfilingScope("SetupWorld"))
            {
                _outputs.Clear();

                World world = Engine.GlobalSingleton.SceneManager.World;

                FindCamerasJob job1 = new FindCamerasJob(this);

                world.InlineEntityQuery<FindCamerasJob, EntityEnabled, Camera, CameraProjectionData>(FindCamerasJob.Query, ref job1);
            }
        }

        internal ReadOnlySpan<RenderOutputData> Outputs => _outputs.AsSpan();

        private readonly record struct FindCamerasJob(RenderWorld World) : IForEachWithEntity<EntityEnabled, Camera, CameraProjectionData>
        {
            public void Update(Entity entity, ref EntityEnabled enabled, ref Camera camera, ref CameraProjectionData projectionData)
            {
                if (enabled.Enabled)
                {
                    World._outputs.Add(new RenderOutputData(entity, camera, projectionData, WindowManager.Instance.PrimaryWindow!));
                }
            }

            public static readonly QueryDescription Query = new QueryDescription().WithAll<EntityEnabled, Camera,CameraProjectionData>();
        }
    }

    internal readonly record struct RenderOutputData(SceneEntity Entity, Camera Camera, CameraProjectionData ProjectionData, Window Window);
}
