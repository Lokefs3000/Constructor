using Primary.Mathematics;
using System.Numerics;

namespace Editor.GeoEdit
{
    internal static class GeoMathUtility
    {
        //https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
        public static Vector3? IntersectTriangle(ref readonly Ray ray, ref readonly Triangle triangle)
        {
            Vector3 edge1 = triangle.B - triangle.A;
            Vector3 edge2 = triangle.C - triangle.A;
            Vector3 rayCrossE2 = Vector3.Cross(ray.Direction, edge2);
            float det = Vector3.Dot(edge1, rayCrossE2);

            if (det > -float.Epsilon && det < float.Epsilon)
                return null;

            float invDet = 1.0f / det;
            Vector3 s = ray.Origin - triangle.A;
            float u = invDet * Vector3.Dot(s, rayCrossE2);

            if ((u < 0.0f && MathF.Abs(u) > float.Epsilon) || (u > 1.0f && MathF.Abs(u - 1.0f) > float.Epsilon))
                return null;

            Vector3 sCrossE1 = Vector3.Cross(s, edge1);
            float v = invDet * Vector3.Dot(ray.Direction, sCrossE1);

            if ((v < 0.0f && MathF.Abs(v) > float.Epsilon) || (u + v > 1.0f && MathF.Abs(u + v - 1.0f) > float.Epsilon))
                return null;

            float t = invDet * Vector3.Dot(edge2, sCrossE1);
            if (t > float.Epsilon)
                return ray.Origin + ray.Direction * t;

            return null;
        }
    }

    internal readonly record struct Triangle(Vector3 A, Vector3 B, Vector3 C);
}
