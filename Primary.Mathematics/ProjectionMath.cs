using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.Mathematics
{
    public static class ProjectionMath
    {
        // https://stackoverflow.com/a/7748548
        public static (Vector2 Position, bool IsBehindViewer) WorldToScreen(Vector3 position, Matrix4x4 vpMatrix, Vector2 screenSize)
        {
            Vector4 projected = Vector4.Transform(new Vector4(position, 1.0f), vpMatrix);
            Vector3 screenSpace = (projected / projected.W).AsVector3();
            Vector2 output = (new Vector2(screenSpace.X, -screenSpace.Y) + Vector2.One) * screenSize * 0.5f;

            return (output, projected.Z < 0.0f);
        }

        public static Ray ViewportToRay(Vector2 position, Matrix4x4 invertedVpMatrix, Vector3 viewOrigin)
        {
            Vector4 projected = Vector4.Transform(new Vector4(position, 0.0f, 1.0f), invertedVpMatrix);
            projected /= projected.W;

            Vector3 direction = Vector3.Normalize(projected.AsVector3() - viewOrigin);

            return new Ray(viewOrigin, direction);
        }
    }

    public readonly record struct WorldToScreenData(Vector2 Position, bool IsBehindViewer);
}
