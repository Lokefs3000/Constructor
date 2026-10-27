using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

namespace Primary.Mathematics
{
    /// <summary>Plagerized from <see cref="BoundingFrustum"/> since i wan't to avoid reallocating the the array every frame</summary>
    public struct Frustrum
    {
        public Plane Near;
        public Plane Far;
        public Plane Left;
        public Plane Right;
        public Plane Top;
        public Plane Bottom;

        public Frustrum(Matrix4x4 viewProjection)
        {
            Near = Plane.Normalize(new Plane(-viewProjection.M13, -viewProjection.M23, -viewProjection.M33, -viewProjection.M43));
            Far = Plane.Normalize(new Plane(viewProjection.M13 - viewProjection.M14, viewProjection.M23 - viewProjection.M24, viewProjection.M33 - viewProjection.M34, viewProjection.M43 - viewProjection.M44));
            Left = Plane.Normalize(new Plane(-viewProjection.M14 - viewProjection.M11, -viewProjection.M24 - viewProjection.M21, -viewProjection.M34 - viewProjection.M31, -viewProjection.M44 - viewProjection.M41));
            Right = Plane.Normalize(new Plane(viewProjection.M11 - viewProjection.M14, viewProjection.M21 - viewProjection.M24, viewProjection.M31 - viewProjection.M34, viewProjection.M41 - viewProjection.M44));
            Top = Plane.Normalize(new Plane(viewProjection.M12 - viewProjection.M14, viewProjection.M22 - viewProjection.M24, viewProjection.M32 - viewProjection.M34, viewProjection.M42 - viewProjection.M44));
            Bottom = Plane.Normalize(new Plane(-viewProjection.M14 - viewProjection.M12, -viewProjection.M24 - viewProjection.M22, -viewProjection.M34 - viewProjection.M32, -viewProjection.M44 - viewProjection.M42));
        }

        public readonly bool Intersects(AABB aabb)
        {
            return !(IsInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Near) ||
                IsInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Far) ||
                IsInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Left) ||
                IsInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Right) ||
                IsInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Top) ||
                IsInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Bottom));
        }

        public readonly FrustrumIntersection IntersectWhere(AABB aabb)
        {
            bool value = IsFullyInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Near);

            if (IsFullyInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Far) != value)
                return FrustrumIntersection.Partial;
            if (IsFullyInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Left) != value)
                return FrustrumIntersection.Partial;
            if (IsFullyInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Right) != value)
                return FrustrumIntersection.Partial;
            if (IsFullyInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Top) != value)
                return FrustrumIntersection.Partial;
            if (IsFullyInFrontOfPlane(in aabb.Minimum, in aabb.Maximum, in Bottom) != value)
                return FrustrumIntersection.Partial;

            return value ? FrustrumIntersection.Outside : FrustrumIntersection.Inside;
        }

        public readonly bool Intersects(Vector3 center, float radius)
        {
            return !(
                IsInFrontOfPlane(in center, in radius, in Near) ||
                IsInFrontOfPlane(in center, in radius, in Far) ||
                IsInFrontOfPlane(in center, in radius, in Left) ||
                IsInFrontOfPlane(in center, in radius, in Right) ||
                IsInFrontOfPlane(in center, in radius, in Top) ||
                IsInFrontOfPlane(in center, in radius, in Bottom));
        }

        private static bool IsInFrontOfPlane(in Vector3 min, in Vector3 max, in Plane plane)
        {
            Vector128<float> normalSimd = plane.AsVector128();

            Vector128<float> aabbMin = min.AsVector128();
            Vector128<float> aabbMax = max.AsVector128();

            Vector128<float> aabbCenter = (aabbMin + aabbMax) * 0.5f;
            Vector128<float> aabbExtents = (aabbMax - aabbMin) * 0.5f;

            float d = Vector128.Dot(aabbCenter, plane.AsVector128());
            float r = Vector128.Dot(aabbExtents, Vector128.Abs(plane.AsVector128()));

            return d - r > -plane.D;
        }

        private static bool IsFullyInFrontOfPlane(in Vector3 min, in Vector3 max, in Plane plane)
        {
            Vector128<float> normalSimd = plane.AsVector128();

            Vector128<float> aabbMin = min.AsVector128();
            Vector128<float> aabbMax = max.AsVector128();

            Vector128<float> aabbCenter = (aabbMin + aabbMax) * 0.5f;
            Vector128<float> aabbExtents = (aabbMax - aabbMin) * 0.5f;

            float d = Vector128.Dot(aabbCenter, plane.AsVector128());
            float r = Vector128.Dot(aabbExtents, Vector128.Abs(plane.AsVector128()));

            return d + r > -plane.D;
        }

        private static bool IsInFrontOfPlane(in Vector3 center, in float radius, in Plane plane)
        {
            float distance = Vector128.Dot(plane.Normal.AsVector128(), center.AsVector128());
            return distance + plane.D > radius;
        }
    }

    public enum FrustrumIntersection : byte
    {
        Outside = 0,
        Partial,
        Inside
    }
}
