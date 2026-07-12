using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Common;
using Primary.Common;

namespace EditorUI.Serialization.Value
{
    [ValueConverter]
    internal sealed class UIColorValueConverter : ValueConverter<UIColor>
    {
        public override UIColor TryDeserialize(ReadOnlySpan<char> source)
        {
            if (source.IsEmpty)
                throw new Exception("Expected value");

            if (source.SequenceEqual("none"))
                return Color.TransparentBlack;

            if (source[0] == '#')
                return Color.FromHex(source[1..]);

            if (source.StartsWith("rgb(") && source[^1] == ')')
            {
                var tokenizer = source[4..^1].Tokenize(',');
                Color color = Color.Black;

                for (int i = 0; i < 4; ++i)
                {
                    if (!tokenizer.MoveNext())
                    {
                        if (i == 3)
                            return color;
                        throw new Exception("Expected value");
                    }

                    color[i] = tokenizer.Current.Contains('.') ?
                        float.Parse(tokenizer.Current, CultureInfo.InvariantCulture) :
                        int.Parse(tokenizer.Current, CultureInfo.InvariantCulture) / 255.0f;
                }

                if (tokenizer.MoveNext())
                    throw new Exception("Too many values");
                return color;
            }
            else if (source.StartsWith("hsv(") && source[^1] == ')')
            {
                var tokenizer = source[4..^1].Tokenize(',');

                tokenizer.MoveNext();
                float hue = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

                tokenizer.MoveNext();
                float saturation = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

                tokenizer.MoveNext();
                float value = float.Parse(tokenizer.Current, CultureInfo.InvariantCulture);

                float alpha = 1.0f;
                if (tokenizer.MoveNext())
                {
                    alpha = tokenizer.Current.Contains('.') ?
                       float.Parse(tokenizer.Current, CultureInfo.InvariantCulture) :
                       int.Parse(tokenizer.Current, CultureInfo.InvariantCulture) / 255.0f;
                }

                if (tokenizer.MoveNext())
                    throw new Exception("Too many values");
                return Color.FromHSV(hue, saturation, value, alpha);
            }
            else if (Color.TryGetColorFromName(source, out Color color))
            {
                return color;
            }

            throw new NotImplementedException();
        }

        public override string TrySerialize(UIColor value)
        {
            if (value.Type == ColorType.Solid)
            {
                Color rgba = Color.Clamp(value.Solid);
                return rgba.A < 1.0f ?
                    $"rgb({rgba.R.ToString(CultureInfo.InvariantCulture)},{rgba.G.ToString(CultureInfo.InvariantCulture)},{rgba.B.ToString(CultureInfo.InvariantCulture)},{rgba.A.ToString(CultureInfo.InvariantCulture)})" :
                    $"rgb({rgba.R.ToString(CultureInfo.InvariantCulture)},{rgba.G.ToString(CultureInfo.InvariantCulture)},{rgba.B.ToString(CultureInfo.InvariantCulture)})";
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
