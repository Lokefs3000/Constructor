using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Arch.Core;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Components;
using Primary.Mathematics;
using Primary.Threading;
using VoxelizationDemo.Components;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Rendering
{
    public sealed class LightCollector
    {
        private readonly List<PointLightData> _pointLights;

        internal LightCollector()
        {
            _pointLights = new List<PointLightData>();
        }

        internal void CollectLights(Frustrum frustrum, Vector3 focus, ref RentedList<JobHandle> jobHandleList)
        {
            CollectPointLightsJob.Instance.PointLights = _pointLights;
            CollectPointLightsJob.Instance.Frustrum = frustrum;
            CollectPointLightsJob.Instance.Focus = focus;
            jobHandleList.Add(JobScheduler.Schedule(CollectPointLightsJob.Instance, JobPriority.Realtime));
        }

        public ROList<PointLightData> PointLights => _pointLights;

        private sealed class CollectPointLightsJob : IJob
        {
            public List<PointLightData>? PointLights;
            public Frustrum Frustrum;
            public Vector3 Focus;

            public void Execute()
            {
                if (PointLights == null)
                    return;

                PointLights.Clear();

                World world = VoxelRuntime.Instance.SceneManager.World;
                Query query = world.Query(in s_queryPointLights);

                foreach (ref Chunk chunk in query)
                {
                    ref EntityEnabled firstEntityEnabled = ref chunk.GetFirst<EntityEnabled>();
                    ref WorldTransform firstTransformElem = ref chunk.GetFirst<WorldTransform>();
                    ref PointLight firstPointLightElem = ref chunk.GetFirst<PointLight>();

                    foreach (int entityIndex in chunk)
                    {
                        ref EntityEnabled enabled = ref Unsafe.Add(ref firstEntityEnabled, entityIndex);
                        if (enabled.Enabled)
                        {
                            ref WorldTransform transform = ref Unsafe.Add(ref firstTransformElem, entityIndex);
                            ref PointLight pointLight = ref Unsafe.Add(ref firstPointLightElem, entityIndex);

                            if (pointLight.Intensity > 0.0f && pointLight.Radius > 0.0f)
                            {
                                PointLights.Add(new PointLightData(
                                    transform.Transformation.Translation,
                                    pointLight.Radius,
                                    pointLight.Diffuse.AsVector4().WithElement(3, pointLight.Intensity),
                                    pointLight.Specular.AsVector4().WithElement(3, pointLight.Falloff)));
                            }
                        }
                    }
                }

                PointLights = null;
            }

            private static readonly QueryDescription s_queryPointLights = new QueryDescription()
                .WithAll<EntityEnabled, WorldTransform, PointLight>();

            public static readonly CollectPointLightsJob Instance = new CollectPointLightsJob();
        }
    }

    public readonly record struct PointLightData(Vector3 Position, float Radius, Vector4 Diffuse, Vector4 Specular);
}
