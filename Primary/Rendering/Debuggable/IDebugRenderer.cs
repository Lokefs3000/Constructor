using Primary.Common;
using System.Numerics;

namespace Primary.Rendering.Debuggable
{
    public interface IDebugRenderer
    {
        public void DrawLine(Vector3 from, Vector3 to, Color color);
        public void DrawVector(Vector3 position);

        public void DrawWireSphere(Vector3 center, float radius, Color color);
        public void DrawWireBox(Vector3 center, Vector3 extents, Color color) => DrawWireAABB(AABB.FromExtents(center, extents), color);
        public void DrawWireAABB(AABB aabb, Color color);
        public void DrawWireCircle(Vector3 center, float radius, Color color);
        public void DrawWireBoundaries(Boundaries boundaries, Color color) => DrawWireRect(boundaries.Minimum, boundaries.Maximum, color);
        public void DrawWireRect(Vector2 min, Vector2 max, Color color);

        public void DrawSolidRect(Vector2 min, Vector2 max, Color color);

        public void PushMatrix(Matrix4x4 matrix, bool multiplyWithPrevious);
        public void PopMatrix();
    }
}
