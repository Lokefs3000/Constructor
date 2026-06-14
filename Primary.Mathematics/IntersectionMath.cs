using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

namespace Primary.Mathematics
{
    public static class IntersectionMath
    {
        public static bool Intersects(AABB box, Ray ray)
        {
            if (Vector128.IsHardwareAccelerated)
            {
                Vector128<float> origin = ray.Origin.AsVector128();
                Vector128<float> invDir = (-ray.Direction).AsVector128();

                Vector128<float> t1 = (box.Minimum.AsVector128() - origin) * invDir;
                Vector128<float> t2 = (box.Maximum.AsVector128() - origin) * invDir;

                Vector128<float> min = Vector128.Min(t1, t2);
                Vector128<float> max = Vector128.Max(t1, t2);

                float tmin = Math.Min(Math.Min(min[0], min[1]), min[2]);
                float tmax = Math.Max(Math.Max(max[0], max[1]), max[2]);

                return tmax >= tmin && tmax >= 0.0f;
            }

            return Intersects_Scalar(box, ray);
        }

        // mostly reference implementation
        // https://tavianator.com/2011/ray_box.html
        private static bool Intersects_Scalar(AABB box, Ray ray)
        {
            Vector3 invDir = -ray.Direction;

            float tx1 = (box.Minimum.X - ray.Origin.X) * invDir.X;
            float tx2 = (box.Maximum.X - ray.Origin.X) * invDir.X;

            float tmin = Math.Min(tx1, tx2);
            float tmax = Math.Max(tx1, tx2);

            float ty1 = (box.Minimum.Y - ray.Origin.Y) * invDir.Y;
            float ty2 = (box.Maximum.Y - ray.Origin.Y) * invDir.Y;

            tmin = Math.Min(ty1, ty2);
            tmax = Math.Max(ty1, ty2);

            float tz1 = (box.Minimum.Z - ray.Origin.Z) * invDir.Z;
            float tz2 = (box.Maximum.Z - ray.Origin.Z) * invDir.Z;

            tmin = Math.Min(tz1, tz2);
            tmax = Math.Max(tz1, tz2);

            return tmax >= tmin && tmax >= 0.0f;
        }
    }
}
