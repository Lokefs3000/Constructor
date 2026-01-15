using Primary.Common;
using CommunityToolkit.HighPerformance;

namespace Editor.UI.Datatypes
{
    public struct UIGradientColor
    {
        public UIGradientType Type;
        public UIGradientKey[] Keys;

        public float Rotation;

        public UIGradientColor()
        {
            Type = UIGradientType.Linear;
            Keys = Array.Empty<UIGradientKey>();

            Rotation = 0.0f;
        }

        public UIGradientColor(UIGradientType type, Color color)
        {
            Type = type;
            Keys = [new UIGradientKey(0.0f, color), new UIGradientKey(1.0f, color)];

            Rotation = 0.0f;
        }

        public UIGradientColor(UIGradientType type, Color from, Color to)
        {
            Type = type;
            Keys = [new UIGradientKey(0.0f, from), new UIGradientKey(1.0f, to)];

            Rotation = 0.0f;
        }

        public UIGradientColor(UIGradientType type, UIGradientKey[] keys)
        {
            Type = type;
            Keys = keys;

            Rotation = 0.0f;
        }

        public override int GetHashCode()
        {
            int hash = Type.GetHashCode();
            for (int i = 0; i < Keys.Length; ++i)
                hash ^= Keys.DangerousGetReferenceAt(i).GetHashCode();

            return hash;
        }
    }

    public struct UIGradientKey
    {
        public float Time;
        public Color Color;

        public UIGradientKey(float time, Color color)
        {
            Time = time;
            Color = color;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Time, Color);
        }
    }

    public enum UIGradientType : byte
    {
        Linear = 0,
        Radial
    }
}
