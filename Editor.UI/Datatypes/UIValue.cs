using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace Editor.UI.Datatypes
{
    public struct UIValue : IEquatable<UIValue>
    {
        public int Absolute;
        public float Relative;

        public UIValue()
        {
            Absolute = 0;
            Relative = 0.0f;
        }

        public UIValue(int absolute)
        {
            Absolute = absolute;
            Relative = 0.0f;
        }

        public UIValue(float relative)
        {
            Absolute = 0;
            Relative = relative;
        }

        public UIValue(int absolute, float relative)
        {
            Absolute = absolute;
            Relative = relative;
        }

        public float Evaluate(float value) => Absolute + Relative * value;

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is UIValue v && Equals(v);
        public bool Equals(UIValue other) => Absolute == other.Absolute && Relative == other.Relative;

        public override int GetHashCode() => HashCode.Combine(Absolute, Relative);
        public override string ToString() => $"<{Absolute}, {Relative:P}>";

        public static UIValue operator+(UIValue left, UIValue right) => new UIValue(left.Absolute + right.Absolute, left.Relative + right.Relative);
        public static UIValue operator-(UIValue left, UIValue right) => new UIValue(left.Absolute - right.Absolute, left.Relative - right.Relative);
        
        public static UIValue operator+(UIValue left, float right) => new UIValue(left.Absolute, left.Relative + right);
        public static UIValue operator-(UIValue left, float right) => new UIValue(left.Absolute, left.Relative - right);

        public static UIValue operator +(UIValue left, int right) => new UIValue(left.Absolute + right, left.Relative);
        public static UIValue operator -(UIValue left, int right) => new UIValue(left.Absolute - right, left.Relative);

        public static bool operator ==(UIValue left, UIValue right) => left.Equals(right);
        public static bool operator !=(UIValue left, UIValue right) => !left.Equals(right);

        public static readonly UIValue Zero = new UIValue();
        public static readonly UIValue Max = new UIValue(1.0f);
    }
}
