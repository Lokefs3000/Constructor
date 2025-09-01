using System.Numerics;

namespace Editor.LegacyGui.Data
{
    public struct Boundaries
    {
        public Vector2 Minimum;
        public Vector2 Maximum;

        public Boundaries()
        {
            Minimum = Vector2.Zero;
            Maximum = Vector2.Zero;
        }

        public Boundaries(Vector2 min, Vector2 max)
        {
            Minimum = min;
            Maximum = max;
        }

        public Vector2 Size => Maximum - Minimum;
        public Vector2 Center => Vector2.Lerp(Minimum, Maximum, 0.5f);

        public static readonly Boundaries Zero = new Boundaries();
    }
}
