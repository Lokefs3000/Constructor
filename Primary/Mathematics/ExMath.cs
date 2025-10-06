using System.Numerics;

namespace Primary.Mathematics
{
    public static class ExMath
    {
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
    }
}
