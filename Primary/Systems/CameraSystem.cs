using Arch.Core;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Scenes;
using Schedulers;
using System.Numerics;

namespace Primary.Systems
{
    public struct CameraSystem : ISystem, IForEach<Camera, CameraProjectionData, WorldTransform>
    {
        public void Schedule(World world, JobScheduler scheduler)
        {
            using (new ProfilingScope("Camera"))
            {
                SceneManager manager = Engine.GlobalSingleton.SceneManager;
                manager.World.InlineQuery<CameraSystem, Camera, CameraProjectionData, WorldTransform>(s_query, ref this);
            }
        }

        public void Update(ref Camera camera, ref CameraProjectionData projectionData, ref WorldTransform transform)
        {
            Vector2 clientSize = Vector2.Zero;
            if (true)
            {
                Window? window = WindowManager.Instance.PrimaryWindow!;
                if (window != null)
                    clientSize = window.ClientSize;
            }

            if (camera.IsDirty || projectionData.ClientSize != clientSize)
            {
                projectionData.ClientSize = clientSize;
                projectionData.ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(camera.FieldOfView, clientSize.X / clientSize.Y, camera.NearClip, camera.FarClip);

                camera.IsDirty = false;
            }

            projectionData.ViewMatrix = Matrix4x4.CreateLookTo(
                transform.Transformation.Translation,
                new Vector3(transform.Transformation.M31, transform.Transformation.M32, transform.Transformation.M33),
                new Vector3(transform.Transformation.M21, transform.Transformation.M22, transform.Transformation.M23));
        }

        private static readonly QueryDescription s_query = new QueryDescription().WithAll<Camera, CameraProjectionData, Transform>();

        public ref readonly QueryDescription Description => ref s_query;
        public bool SystemNeedsFullExecutionTime => false;
    }
}
