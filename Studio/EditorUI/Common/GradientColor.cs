using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;

namespace EditorUI.Common
{
    public record struct GradientColor
    {
        public GradientType Type;
        public GradientKey[] Keys;

        public GradientColor(GradientType type = GradientType.Linear)
        {
            Type = type;
            Keys = [];
        }

        public GradientColor(Color color, GradientType type = GradientType.Linear)
        {
            Type = type;
            Keys = [
                new GradientKey(0.0f, color)
                ];
        }

        public GradientColor(Color from, Color to, GradientType type = GradientType.Linear)
        {
            Type = type;
            Keys = [
                new GradientKey(0.0f, from),
                new GradientKey(1.0f, to)
                ];
        }

        public GradientColor(params GradientKey[] keys)
        {
            Type = GradientType.Linear;
            Keys = keys;
        }

        public GradientColor(GradientType type, params GradientKey[] keys)
        {
            Type = type;
            Keys = keys;
        }

        public GradientColor(params Color[] colors)
        {
            Type = GradientType.Linear;
            Keys = [.. colors.Select((col, i) => new GradientKey(i / (float)(colors.Length - 1), col))];
        }

        public GradientColor(GradientType type, params Color[] colors)
        {
            Type = type;
            Keys = [.. colors.Select((col, i) => new GradientKey(i / (float)(colors.Length - 1), col))];
        }

        public readonly Color Sample(float time)
        {
            switch (Keys.Length)
            {
                case 0: return Color.Black;
                case 1: return Keys[0].Color;
                default:
                    {
                        time = Math.Clamp(time, 0.0f, 1.0f);

                        Span<GradientKey> keys = Keys.AsSpan();
                        for (int i = 1; i < keys.Length; i++)
                        {
                            ref GradientKey key = ref keys[i];
                            if (key.Time >= time)
                            {
                                ref GradientKey prevKey = ref keys[i - 1];
                                return Color.Lerp(prevKey.Color, key.Color, (time - prevKey.Time) / (key.Time - prevKey.Time));
                            }
                        }

                        return keys[^1].Color;
                    }
            }
        }

        public readonly override int GetHashCode() => HashCode.Combine(Type, Keys);
    }

    public enum GradientType : byte
    {
        Linear = 0,
        Radial
    }

    public record struct GradientKey
    {
        public float Time;
        public Color Color;

        public GradientKey(float time, Color color)
        {
            Time = time;
            Color = color;
        }
    }
}
