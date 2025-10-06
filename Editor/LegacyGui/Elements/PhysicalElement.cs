using Editor.LegacyGui.Data;
using System.Numerics;

namespace Editor.LegacyGui.Elements
{
    public class PhysicalElement : Element
    {
        protected DecoratedValue<Vector2> _position;
        protected DecoratedValue<Vector2> _size;

        public PhysicalElement()
        {
            _position = Vector2.Zero;
            _size = Vector2.Zero;
        }

        public override bool RecalculateLayout()
        {
            _position.Undecorate();
            _size.Undecorate();

            if (Parent is PhysicalElement physical)
                _position.Decorate(_position.Decorated + physical.Position.Decorated);

            return base.RecalculateLayout();
        }

        internal void SetBoundaries(Boundaries boundaries)
        {
            _position.Value = boundaries.Minimum;
            _size.Value = boundaries.Maximum;

            _position.Undecorate();
            _size.Undecorate();

            InvalidateLayout();
        }

        public ref readonly DecoratedValue<Vector2> Position => ref _position;
        public ref readonly DecoratedValue<Vector2> Size => ref _size;
    }
}
