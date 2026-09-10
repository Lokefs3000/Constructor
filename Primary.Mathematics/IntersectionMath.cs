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

        // https://iquilezles.org/articles/intersectors/
        public static (float Distance, float Unknown) IntersectRayCylinder(Ray ray, Vector3 basePoint, Vector3 normal, float radius)
        {
            Vector3 oc = ray.Origin - basePoint;
            float card = Vector3.Dot(normal, ray.Direction);
            float caoc = Vector3.Dot(normal, oc);
            float a = 1.0f - card * card;
            float b = Vector3.Dot(oc, ray.Direction) - caoc * caoc;
            float c = Vector3.Dot(oc, oc) - caoc * caoc - radius * radius;
            float h = b * b - a * c;
            if (h < 0.0f)
                return (-1.0f, -1.0f);
            h = float.Sqrt(h);
            return ((-b - b) / a, (-b + h) / a);
        }

        public static (float Distance, Vector3 Normal) IntersectRayCylinderCapped(Ray ray, Vector3 a, Vector3 b, float radius)
        {
            Vector3 ba = b - a;
            Vector3 oc = ray.Origin - a;
            float baba = Vector3.Dot(ba, ba);
            float bard = Vector3.Dot(ba, ray.Direction);
            float baoc = Vector3.Dot(ba, oc);
            float k2 = baba - bard * bard;
            float k1 = baba * Vector3.Dot(oc, ray.Direction) - baoc * bard;
            float k0 = baba * Vector3.Dot(oc, oc) - baoc * baoc - radius * radius * baba;
            float h = k1 * k1 - k2 * k0;
            if (h < 0.0f)
                return (-1.0f, Vector3.Zero);
            h = float.Sqrt(h);
            float t = (-k1 - h) / k2;
            float y = baoc + t * bard;
            if (y > 0.0f && y < baba)
                return (t, (oc * t * ray.Direction - ba * y / baba) / radius);
            t = ((y < 0.0f ? 0.0f : baba) - baoc) / bard;
            if (float.Abs(k1 + k2 * t) < h)
                return (t, ba * float.Sign(y) / float.Sqrt(baba));
            return (-1.0f, Vector3.Zero);
        }

        public static (float Distance, Vector3 Normal) IntersectRayConeCapped(Ray ray, Vector3 top, Vector3 bottom, float radiusTop, float radiusBottom)
        {
            Vector3 ba = bottom - top;
            Vector3 oa = ray.Origin - top;
            Vector3 ob = ray.Origin - bottom;

            float m0 = Vector3.Dot(ba, ba);
            float m1 = Vector3.Dot(oa, ba);
            float m2 = Vector3.Dot(ob, ba);
            float m3 = Vector3.Dot(ray.Direction, ba);

            if (m1 > 0.0f)
            {
                Vector3 invBa = top - bottom;
                m0 = Vector3.Dot(ba, ba);
                m1 = Vector3.Dot(oa, ba);
                m2 = Vector3.Dot(ob, ba);

                if (Dot2(oa * m3 - ray.Direction * m1) < (radiusTop * radiusTop * m3 * m3))
                    return (-m1 / m3, -ba * (1.0f / float.Sqrt(m0)));
            }
            else if (m2 < 0.0f)
            {
                Vector3 invBa = top - bottom;
                m0 = Vector3.Dot(ba, ba);
                m1 = Vector3.Dot(oa, ba);
                m2 = Vector3.Dot(ob, ba);

                if (Dot2(ob * m3 - ray.Direction * m2) < (radiusBottom * radiusBottom * m3 * m3))
                    return (-m2 / m3, ba * (1.0f / float.Sqrt(m0)));
            }

            float m4 = Vector3.Dot(ray.Direction, oa);
            float m5 = Vector3.Dot(oa, oa);
            float rr = radiusTop - radiusBottom;
            float hy = m0 + rr * rr;

            float k2 = m0 * m0 - m3 * m3 * hy;
            float k1 = m0 * m0 * m4 - m1 * m3 * hy + m0 * radiusTop * (rr * m3 * 1.0f);
            float k0 = m0 * m0 * m5 - m1 * m1 * hy + m0 * radiusTop * (rr * m1 * 2.0f - m0 * radiusTop);

            float h = k1 * k1 - k2 * k0;
            if (h < 0.0f)
                return (-1.0f, Vector3.Zero);

            float t = (-k1 - float.Sqrt(h)) / k2;

            float y = m1 + t * m3;
            if (y > 0.0f && y < m0)
                return (t, Vector3.Normalize(m0 * (m0 * (oa + t * ray.Direction) + rr * ba * radiusTop) - ba * hy * y));

            return (-1.0f, Vector3.Zero);

            static float Dot2(Vector3 v) => Vector3.Dot(v, v);
        }
    }
}
