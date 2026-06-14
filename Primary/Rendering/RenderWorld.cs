using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using Primary.Components;
using Primary.Profiling;
using Primary.RHI;
using Primary.Scenes;
using Primary.Utility;
using Primary.Windowing;

namespace Primary.Rendering
{
    public sealed class RenderWorld
    {
        private EventList<List<RenderOutputData>> _outputTransformers;

        private List<RenderOutputData> _outputs;

        internal RenderWorld()
        {
            _outputTransformers = new EventList<List<RenderOutputData>>();

            _outputs = new List<RenderOutputData>();
        }

        internal void SetupWorld()
        {
            using (new ProfilingScope("SetupWorld"))
            {
                _outputs.Clear();

                World world = Engine.GlobalSingleton.SceneManager.World;

                FindCamerasJob job1 = new FindCamerasJob(this);

                world.InlineEntityQuery<FindCamerasJob, EntityEnabled, Camera, WorldTransform, CameraProjectionData>(FindCamerasJob.Query, ref job1);

                _outputTransformers.Invoke(_outputs);
                _outputs.Sort((a, b) => a.Window.WindowId.CompareTo(b.Window.WindowId));
            }
        }

        internal ReadOnlySpan<RenderOutputData> Outputs => _outputs.AsSpan();

        public ROEventList<List<RenderOutputData>> TransformOutput => _outputTransformers;

        private readonly record struct FindCamerasJob(RenderWorld World) : IForEachWithEntity<EntityEnabled, Camera, WorldTransform, CameraProjectionData>
        {
            public void Update(Entity entity, ref EntityEnabled enabled, ref Camera camera, ref WorldTransform transform, ref CameraProjectionData projectionData)
            {
                if (enabled.Enabled && camera.UpdateMode == CameraUpdateMode.EveryFrame)
                {
                    Window? outputWindow;

                    ref CameraOutput output = ref entity.TryGetRef<CameraOutput>(out bool exists);
                    if (exists)
                        outputWindow = WindowManager.Instance.FindWindow(output.WindowId);
                    else
                        outputWindow = WindowManager.Instance.PrimaryWindow;

                    if (outputWindow != null && outputWindow.IsShown)
                        World._outputs.Add(new RenderOutputData(entity, transform, camera, projectionData, outputWindow, null));
                }
            }

            public static readonly QueryDescription Query = new QueryDescription().WithAll<EntityEnabled, Camera, WorldTransform, CameraProjectionData>();
        }
    }

    public readonly record struct RenderOutputData(SceneEntity Entity, WorldTransform Transform, Camera Camera, CameraProjectionData ProjectionData, Window Window, RHITexture? TargetTexture);
}
