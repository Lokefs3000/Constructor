using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Components;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Threading;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Rendering.Icons
{
    internal sealed class WorldIconManager
    {
        private readonly List<WorldIconSource> _iconSources;

        private int _totalIconCount;

        internal WorldIconManager()
        {
            _iconSources = [
                new PointLightIcons()
                ];

            _totalIconCount = 0;
        }

        internal void CollectAllVisibleIcons()
        {
            using (new ProfilingScope("WorldIcons"))
            {
                ref WorldTransform worldTransform = ref VoxelRuntime.Instance.CameraManager.Entity.GetComponent<WorldTransform>();
                ref CameraProjectionData projectionData = ref VoxelRuntime.Instance.CameraManager.Entity.GetComponent<CameraProjectionData>();

                Frustrum frustrum = new Frustrum(projectionData.ViewMatrix * projectionData.ProjectionMatrix);

                using RentedList<JobHandle> handles = new RentedList<JobHandle>(_iconSources.Count);
                foreach (WorldIconSource source in _iconSources)
                {
                    source.SetParameters(frustrum, worldTransform.Position);
                    handles.Add(JobScheduler.Schedule(source, JobPriority.Realtime));
                }

                JobHandle handle = JobScheduler.CombineAll(handles.AsSpan());
                JobScheduler.Flush(handles.AsSpan());

                handle.WaitForCompletion();

                _totalIconCount = 0;
                foreach (WorldIconSource source in _iconSources)
                {
                    _totalIconCount += source.Icons.Count;
                }
            }
        }

        public ROList<WorldIconSource> IconSources => _iconSources;

        public int TotalIconCount => _totalIconCount;
    }

    internal readonly record struct WorldIconData(Vector4 Position, Vector2 UVMin, Vector2 UVMax, Color Color);
}
