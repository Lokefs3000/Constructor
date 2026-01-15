using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.UI.Datatypes
{
    public struct UIValue2 : IEquatable<UIValue2>
    {
        public UIValue X;
        public UIValue Y;

        public UIValue2()
        {
            X = UIValue.Zero;
            Y = UIValue.Zero;
        }

        public UIValue2(int absoluteX, int absoluteY)
        {
            X = new UIValue(absoluteX);
            Y = new UIValue(absoluteY);
        }

        public UIValue2(float relativeX, float relativeY)
        {
            X = new UIValue(relativeX);
            Y = new UIValue(relativeY);
        }

        public UIValue2(UIValue x, UIValue y)
        {
            X = x;
            Y = y;
        }

        public Vector2 Evaluate(Vector2 value)
        {
            Vector2 absolute = new Vector2(X.Absolute, Y.Absolute);
            Vector2 relative = new Vector2(X.Relative, Y.Relative);

            return absolute + relative * value;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is UIValue2 v && Equals(v);
        public bool Equals(UIValue2 other) => X == other.X && Y == other.Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"< {X}, {Y} >";

        //TODO: accelerate using SIMD operations
        public static UIValue2 operator +(UIValue2 left, UIValue2 right) => new UIValue2(left.X + right.X, left.Y + right.Y);
        public static UIValue2 operator -(UIValue2 left, UIValue2 right) => new UIValue2(left.X + right.X, left.Y + right.Y);

        public static UIValue2 operator +(UIValue2 left, Vector2 right) => new UIValue2(left.X + right.X, left.Y + right.Y);
        public static UIValue2 operator -(UIValue2 left, Vector2 right) => new UIValue2(left.X - right.X, left.Y - right.Y);

        public static bool operator ==(UIValue2 left, UIValue2 right) => left.Equals(right);
        public static bool operator !=(UIValue2 left, UIValue2 right) => !left.Equals(right);

        public static readonly UIValue2 Zero = new UIValue2();
        public static readonly UIValue2 Max = new UIValue2(UIValue.Max, UIValue.Max);
    }
}
