using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Primary.Mathematics
{
    public struct Ray : IEquatable<Ray>
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

        public readonly Vector3 AtDistance(float distance) => Origin + Direction * distance;

        public static Ray Normalize(Ray ray) => new Ray(ray.Origin, Vector3.Normalize(ray.Direction));

        public static Ray ViewportToWorld(Matrix4x4 projection, Matrix4x4 view, Vector2 viewport)
        {
            Matrix4x4.Invert(projection, out Matrix4x4 invProj);
            Matrix4x4.Invert(view, out Matrix4x4 invView);

            Vector3 clipspace = new Vector3(viewport.X, viewport.Y, 0.0f);
            Vector3 viewspace = Vector3.Transform(clipspace, invProj);
            Vector3 worldspace = Vector3.Transform(viewspace, invView);

            Vector3 direction = Vector3.Normalize(worldspace - invView.Translation);

            return new Ray(worldspace, direction);
        }

        public static Ray ViewportToWorldInverse(Matrix4x4 projection, Matrix4x4 view, Vector2 viewport)
        {
            Matrix4x4.Invert(projection, out Matrix4x4 invProj);
            Matrix4x4.Invert(view, out Matrix4x4 invView);

            Vector3 clipspace = new Vector3(viewport.X, viewport.Y, 0.0f);
            Vector3 viewspace = Vector3.Transform(clipspace, invProj);
            Vector3 worldspace = Vector3.Transform(viewspace, invView);

            Vector3 direction = Vector3.Normalize(invView.Translation - worldspace);

            return new Ray(worldspace, direction);
        }

        // https://underdisc.net/blog/6_gizmos/index.html
        /// <summary>
        /// Find the closest distance on a ray from another ray.
        /// </summary>
        /// <param name="other">The ray to find the closest distance from</param>
        /// <returns>Closest distance on the ray to the other</returns>
        /// <remarks>Returns 0 if both rays are parallel</remarks>
        public float FindClosest(Ray other)
        {
            Vector3 sd = Origin - other.Origin;
            Vector3 da = Direction;
            Vector3 db = other.Direction;
            float dadb = Vector3.Dot(da, db);
            float dasd = Vector3.Dot(da, sd);
            float dbsd = Vector3.Dot(db, sd);
            float denom = 1.0f - dadb * dadb;
            return denom == 0.0f ? 0.0f : ((-dasd + dadb * dbsd) / denom);
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is Ray v && Equals(v);
        public bool Equals(Ray other) => Origin == other.Origin && Direction == other.Direction;

        public override int GetHashCode() => HashCode.Combine(Origin, Direction);

        public override string ToString() => $"{{Origin:{Origin} Direction:{Direction}}}";
    }
}
