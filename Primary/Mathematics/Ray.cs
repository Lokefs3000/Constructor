using System.Numerics;

namespace Primary.Mathematics
{
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;

        public Ray()
        {
            Origin = Vector3.Zero;
            Direction = Vector3.Zero;
        }

        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }

        public override string ToString() => $"{Origin} - {Direction}";

        public Vector3 AtDistance(float distance) => Origin + Direction * distance;

        public static Ray Normalize(Ray ray) => new Ray(ray.Origin, Vector3.Normalize(ray.Direction));
    }
}
