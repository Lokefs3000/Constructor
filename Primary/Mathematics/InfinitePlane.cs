using System.Numerics;

namespace Primary.Mathematics
{
    public struct InfinitePlane
    {
        public Vector3 Position;
        public Vector3 Normal;

        public InfinitePlane()
        {
            Position = Vector3.Zero;
            Normal = Vector3.UnitY;
        }

        public InfinitePlane(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        public static InfinitePlane Normalize(InfinitePlane plane) => new InfinitePlane(plane.Position, Vector3.Normalize(plane.Normal));

        public static float Intersect(InfinitePlane plane, Ray ray)
        {
            float denom = Vector3.Dot(plane.Normal, ray.Direction);
            if (MathF.Abs(denom) > float.Epsilon)
            {
                return Vector3.Dot(plane.Position - ray.Origin, plane.Normal) / denom;
            }

            return float.MinValue;
        }
    }
}
