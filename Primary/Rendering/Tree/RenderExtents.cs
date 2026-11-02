using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using Primary.Components;
using Primary.Mathematics;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Tree
{
    public readonly record struct RenderExtents(Vector3 Center, Vector3 HalfExtents)
    {
        public readonly AABB AsAABB() => new AABB(Center - HalfExtents, Center + HalfExtents);

        public readonly FrustrumIntersection Intersect(in Frustrum frustrum)
        {
            bool value = IsFullyInFrontOfPlane(in frustrum.Near);

            if (IsFullyInFrontOfPlane(in frustrum.Far) != value)
                return FrustrumIntersection.Partial;
            if (IsFullyInFrontOfPlane(in frustrum.Left) != value)
                return FrustrumIntersection.Partial;
            if (IsFullyInFrontOfPlane(in frustrum.Right) != value)
                return FrustrumIntersection.Partial;
            if (IsFullyInFrontOfPlane(in frustrum.Top) != value)
                return FrustrumIntersection.Partial;
            if (IsFullyInFrontOfPlane(in frustrum.Bottom) != value)
                return FrustrumIntersection.Partial;

            return value ? FrustrumIntersection.Outside : FrustrumIntersection.Inside;
        }

        public readonly bool IntersectAny(in Frustrum frustrum)
        {
            return !(
                IsInFrontOfPlane(in frustrum.Near) ||
                IsInFrontOfPlane(in frustrum.Far) ||
                IsInFrontOfPlane(in frustrum.Left) ||
                IsInFrontOfPlane(in frustrum.Right) ||
                IsInFrontOfPlane(in frustrum.Top) ||
                IsInFrontOfPlane(in frustrum.Bottom));
        }

        public readonly bool IsInFrontOfPlane(in Plane plane)
        {
            Vector128<float> normalSimd = plane.AsVector128();

            Vector128<float> aabbCenter = Center.AsVector128Unsafe();
            Vector128<float> aabbExtents = HalfExtents.AsVector128Unsafe();

            float d = Vector128.Dot(aabbCenter, normalSimd);
            float r = Vector128.Dot(aabbExtents, Vector128.Abs(normalSimd));

            return d - r > -plane.D;
        }

        public readonly bool IsFullyInFrontOfPlane(in Plane plane)
        {
            Vector128<float> normalSimd = plane.AsVector128();

            Vector128<float> aabbCenter = Center.AsVector128Unsafe();
            Vector128<float> aabbExtents = HalfExtents.AsVector128Unsafe();

            float d = Vector128.Dot(aabbCenter, normalSimd);
            float r = Vector128.Dot(aabbExtents, Vector128.Abs(normalSimd));

            return d + r > -plane.D;
        }
    }
}
