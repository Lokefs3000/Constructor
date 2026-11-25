using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Collections;
using Primary.Components;
using Primary.Scenes;
using Primary.Scenes.Components;

namespace Primary.Worlds.Volume
{
    public sealed class VolumeContext<T> where T : WorldVolume, new()
    {
        private readonly List<VolumeData> _volumes;

        internal VolumeContext()
        {
            _volumes = new List<VolumeData>();

            SceneEntityManager.Instance.AddComponentAddedCallback<T>(OnVolumeAdded);
        }

        private void OnVolumeAdded(SceneEntity entity)
        {
            T component = entity.GetComponent<T>();
            foreach (VolumeData volume in _volumes)
            {
                if (volume.Component == component)
                {
                    return;
                }
            }

            _volumes.Add(new VolumeData(component, entity.GetComponent<WorldTransform>().Position));
        }

        public RentedList<VolumeIntersection<T>> IntersectAll(Vector3 origin)
        {
            int currentPriority = int.MinValue;
            float totalInfluence = 0.0f;

            T? globalVolume = null;
            RentedList<VolumeIntersection<T>> intersections = new RentedList<VolumeIntersection<T>>();

            foreach (VolumeData volume in _volumes)
            {
                T component = volume.Component;
                if (component.IsGlobal)
                {
                    if (globalVolume == null || globalVolume.Priority < component.Priority)
                    {
                        globalVolume = component;
                        currentPriority = component.Priority;
                    }
                }
                else if (component.Priority >= currentPriority)
                {
                    if (component.Shape == VolumeShape.Box)
                    {
                        Vector3 delta = Vector3.Abs(volume.Position - origin) - component.BoxExtents;
                        if (Vector3.LessThanOrEqualAll(delta, Vector3.Zero))
                        {
                            if (component.Priority > currentPriority)
                            {
                                currentPriority = component.Priority;
                                totalInfluence = 0.0f;

                                intersections.Clear();
                            }

                            float outsideMax = Math.Max(Math.Max(delta.X, delta.Y), delta.Z);
                            float influence = component.BlendDistance > 0.0f ? Math.Min(-outsideMax  / component.BlendDistance, 1.0f) : 1.0f;
                            
                            intersections.Add(new VolumeIntersection<T>(component, influence));
                            totalInfluence += influence;
                        }
                    }
                    else
                    {
                        float distance = Vector3.Distance(volume.Position, origin);
                        if (distance <= component.SphereRadius)
                        {
                            if (component.Priority > currentPriority)
                            {
                                currentPriority = component.Priority;
                                totalInfluence = 0.0f;

                                intersections.Clear();
                            }

                            float influence = component.BlendDistance > 0.0f ? Math.Min((component.SphereRadius - distance) / component.BlendDistance, 1.0f) : 1.0f;

                            intersections.Add(new VolumeIntersection<T>(component, influence));
                            totalInfluence += influence;
                        }
                    }
                }
            }

            float totalEffectCoverage = 0.0f;

            if (intersections.Count > 1)
            {
                float invTotalInfluence = 1.0f / totalInfluence;
                foreach (ref VolumeIntersection<T> intersection in intersections.AsSpan())
                {
                    float newInfluence = intersection.Influence * invTotalInfluence;
                    intersection = new VolumeIntersection<T>(intersection.Volume, newInfluence);
                    totalEffectCoverage += newInfluence;
                }
            }

            if (globalVolume != null && totalEffectCoverage < 1.0f)
            {
                intersections.Insert(0, new VolumeIntersection<T>(globalVolume, 1.0f - totalEffectCoverage));
            }

            return intersections;
        }

        private readonly record struct VolumeData(T Component, Vector3 Position);
    }

    public readonly record struct VolumeIntersection<T>(T Volume, float Influence) where T : WorldVolume, new();
}
