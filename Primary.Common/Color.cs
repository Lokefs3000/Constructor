using CommunityToolkit.Diagnostics;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Primary.Common
{
    public struct Color : IEquatable<Color>
    {
        public float R;
        public float G;
        public float B;
        public float A;

        public Color(float r, float g, float b, float a = 1.0f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color(int r, int g, int b, int a = 255)
        {
            Vector128<float> rgba = Vector128.Create((float)r, (float)g, (float)b, (float)a);
            rgba /= Vector128.Create(255.0f, 255.0f, 255.0f, 255.0f);

            Unsafe.WriteUnaligned(ref Unsafe.As<Color, byte>(ref this), rgba);
        }

        public Color(float scalar, float a = 1.0f)
        {
            R = scalar;
            G = scalar;
            B = scalar;
            A = a;
        }

        public Color(Vector4 rgba)
        {
            R = rgba.X;
            G = rgba.Y;
            B = rgba.Z;
            A = rgba.W;
        }

        public Color(Color color)
        {
            this = color;
        }

        public Color(uint rgba)
        {
            const float ConvertTo01 = 1.0f / 255.0f;

            Color32 _32 = new Color32(rgba);

            R = _32.R * ConvertTo01;
            G = _32.G * ConvertTo01;
            B = _32.B * ConvertTo01;
            A = _32.A * ConvertTo01;
        }

        public Vector4 AsVector4() => Unsafe.ReadUnaligned<Vector4>(ref Unsafe.As<Color, byte>(ref this));
        public Vector3 AsVector3() => Unsafe.ReadUnaligned<Vector3>(ref Unsafe.As<Color, byte>(ref this));

        public Vector128<float> AsVector128() => Unsafe.ReadUnaligned<Vector128<float>>(ref Unsafe.As<Color, byte>(ref this));

        public Color32 ToColor32()
        {
            Vector128<float> min = Vector128.Create(0.0f, 0.0f, 0.0f, 0.0f);
            Vector128<float> max = Vector128.Create(255.0f, 255.0f, 255.0f, 255.0f);

            Vector128<float> vector = Vector128.Truncate(Vector128.Clamp(AsVector128() * max, min, max));
            Vector128<int> ints = Vector128.ConvertToInt32(vector);

            return new Color32(ints.GetElement(0), ints.GetElement(1), ints.GetElement(2), ints.GetElement(3));
        }

        public Color Darken(float amount)
        {
            // TODO: this is actually horrific just make a *.AsColor() for the Vector128
            return new Color((AsVector128() * (1.0f - amount)).AsVector4());
        }

        public float this[int index]
        {
            get
            {
                Guard.IsLessThan((uint)index, 4);
                return Unsafe.Add(ref Unsafe.As<Color, float>(ref this), index);
            }
            set
            {
                Guard.IsLessThan((uint)index, 4);
                Unsafe.Add(ref Unsafe.As<Color, float>(ref this), index) = value;
            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is Color color && Equals(color);
        public bool Equals(Color color) => Vector128.EqualsAll(AsVector128(), color.AsVector128());

        public override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public override string ToString() => $"<{R},{G},{B},{A}>";

        public static Color Black => new Color(0.0f);
        public static Color White => new Color(1.0f);

        public static Color TransparentWhite => new Color(1.0f, 0.0f);
        public static Color TransparentBlack => new Color(0.0f, 0.0f);

        public static Color Normalize(Color color)
        {
            const float ConvertTo01 = 1.0f / 255.0f;

            Vector128<float> convert = Vector128.Create(ConvertTo01, ConvertTo01, ConvertTo01, ConvertTo01);
            convert *= color.AsVector128();

            return Unsafe.ReadUnaligned<Color>(ref Unsafe.As<Vector128<float>, byte>(ref convert));
        }

        public static Color Clamp(Color color)
        {
            Vector128<float> vector = Vector128.Clamp(color.AsVector128(), Vector128<float>.Zero, Vector128<float>.One);
            return Unsafe.ReadUnaligned<Color>(ref Unsafe.As<Vector128<float>, byte>(ref vector));
        }

        public static Color FromHex(ReadOnlySpan<char> hex)
        {
            int rgba = int.Parse(hex, NumberStyles.HexNumber);
            if (hex.Length != 8)
            {
                rgba <<= 8;
                rgba |= 0x000000ff;
            }
            return new Color((uint)rgba);
        }

        //https://www.splinter.com.au/converting-hsv-to-rgb-colour-using-c/
        public static Color FromHSV(float h, float s, float v, float a = 1.0f)
        {
            float H = h;
            while (H < 0) { H += 360; }
            ;
            while (H >= 360) { H -= 360; }
            ;
            float R, G, B;
            if (v <= 0)
            { R = G = B = 0; }
            else if (s <= 0)
            {
                R = G = B = v;
            }
            else
            {
                float hf = H / 60.0f;
                int i = (int)MathF.Floor(hf);
                float f = hf - i;
                float pv = v * (1 - s);
                float qv = v * (1 - s * f);
                float tv = v * (1 - s * (1 - f));
                switch (i)
                {

                    // Red is the dominant color

                    case 0:
                        R = v;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = v;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = v;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = v;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = v;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = v;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = v;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = v;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = v; // Just pretend its black/white
                        break;
                }
            }

            return new Color(R, G, B, a);
        }

        public static Color Lerp(Color from, Color to, float t)
        {
            Vector128<float> v = Vector128.Lerp(from.AsVector128(), to.AsVector128(), Vector128.Create(t, t, t, t));
            return Unsafe.ReadUnaligned<Color>(ref Unsafe.As<Vector128<float>, byte>(ref v));
        }

        public static bool TryGetColorFromName(ReadOnlySpan<char> name, out Color color) => s_defaultColorsDictSpanAlt.TryGetValue(name, out color);

        public static readonly ImmutableArray<Color> DefaultColors = [
            Color.Purple,
            Color.Green,
            Color.Brown,
            Color.Pink,
            Color.Red,
            Color.LightBlue,
            Color.Teal,
            Color.Orange,
            Color.LightGreen,
            Color.Magenta,
            Color.Yellow,
            Color.SkyBlue,
            Color.Grey,
            Color.LimeGreen,
            Color.LightPurple,
            Color.Violet,
            Color.DarkGreen,
            Color.Turquoise,
            Color.Lavender,
            Color.DarkBlue,
            Color.Tan,
            Color.Cyan,
            Color.Aqua,
            Color.ForestGreen,
            Color.Mauve,
            Color.DarkPurple,
            Color.BrightGreen,
            Color.Maroon,
            Color.Olive,
            Color.Salmon,
            Color.Beige,
            Color.RoyalBlue,
            Color.NavyBlue,
            Color.Lilac,
            Color.HotPink,
            Color.LightBrown,
            Color.PaleGreen,
            Color.Peach,
            Color.OliveGreen,
            Color.DarkPink,
            Color.Periwinkle,
            Color.SeaGreen,
            Color.Lime,
            Color.Indigo,
            Color.Mustard,
            Color.LightPink,
                ];

        private static readonly FrozenDictionary<string, Color> s_defaultColorsDict = new Dictionary<string, Color>
        {
            { "Black", Color.Black },
            { "White", Color.White },
            { "Purple", Color.Purple },
            { "Green", Color.Green },
            { "Brown", Color.Brown },
            { "Pink", Color.Pink },
            { "Red", Color.Red },
            { "LightBlue", Color.LightBlue },
            { "Teal", Color.Teal },
            { "Orange", Color.Orange },
            { "LightGreen", Color.LightGreen },
            { "Magenta", Color.Magenta },
            { "Yellow", Color.Yellow },
            { "SkyBlue", Color.SkyBlue },
            { "Grey", Color.Grey },
            { "LimeGreen", Color.LimeGreen },
            { "LightPurple", Color.LightPurple },
            { "Violet", Color.Violet },
            { "DarkGreen", Color.DarkGreen },
            { "Turquoise", Color.Turquoise },
            { "Lavender", Color.Lavender },
            { "DarkBlue", Color.DarkBlue },
            { "Tan", Color.Tan },
            { "Cyan", Color.Cyan },
            { "Aqua", Color.Aqua },
            { "ForestGreen", Color.ForestGreen },
            { "Mauve", Color.Mauve },
            { "DarkPurple", Color.DarkPurple },
            { "BrightGreen", Color.BrightGreen },
            { "Maroon", Color.Maroon },
            { "Olive", Color.Olive },
            { "Salmon", Color.Salmon },
            { "Beige", Color.Beige },
            { "RoyalBlue", Color.RoyalBlue },
            { "NavyBlue", Color.NavyBlue },
            { "Lilac", Color.Lilac },
            { "HotPink", Color.HotPink },
            { "LightBrown", Color.LightBrown },
            { "PaleGreen", Color.PaleGreen },
            { "Peach", Color.Peach },
            { "OliveGreen", Color.OliveGreen },
            { "DarkPink", Color.DarkPink },
            { "Periwinkle", Color.Periwinkle },
            { "SeaGreen", Color.SeaGreen },
            { "Lime", Color.Lime },
            { "Indigo", Color.Indigo },
            { "Mustard", Color.Mustard },
            { "LightPink", Color.LightPink },
        }.ToFrozenDictionary();

        private static readonly FrozenDictionary<string, Color>.AlternateLookup<ReadOnlySpan<char>> s_defaultColorsDictSpanAlt = s_defaultColorsDict.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    public static partial class Extensions
    {
        extension (Color color)
        {
            /// <summary>#7e1e9c</summary>
            public static Color Purple => new Color(0.49411764705882355f, 0.11764705882352941f, 0.611764705882353f, 1.0f);
            /// <summary>#15b01a</summary>
            public static Color Green => new Color(0.08235294117647059f, 0.6901960784313725f, 0.10196078431372549f, 1.0f);
            /// <summary>#0343df</summary>
            public static Color Blue => new Color(0.011764705882352941f, 0.2627450980392157f, 0.8745098039215686f, 1.0f);
            /// <summary>#ff81c0</summary>
            public static Color Pink => new Color(1.0f, 0.5058823529411764f, 0.7529411764705882f, 1.0f);
            /// <summary>#653700</summary>
            public static Color Brown => new Color(0.396078431372549f, 0.21568627450980393f, 0.0f, 1.0f);
            /// <summary>#e50000</summary>
            public static Color Red => new Color(0.8980392156862745f, 0.0f, 0.0f, 1.0f);
            /// <summary>#95d0fc</summary>
            public static Color LightBlue => new Color(0.5843137254901961f, 0.8156862745098039f, 0.9882352941176471f, 1.0f);
            /// <summary>#029386</summary>
            public static Color Teal => new Color(0.00784313725490196f, 0.5764705882352941f, 0.5254901960784314f, 1.0f);
            /// <summary>#f97306</summary>
            public static Color Orange => new Color(0.9764705882352941f, 0.45098039215686275f, 0.023529411764705882f, 1.0f);
            /// <summary>#96f97b</summary>
            public static Color LightGreen => new Color(0.5882352941176471f, 0.9764705882352941f, 0.4823529411764706f, 1.0f);
            /// <summary>#c20078</summary>
            public static Color Magenta => new Color(0.7607843137254902f, 0.0f, 0.47058823529411764f, 1.0f);
            /// <summary>#ffff14</summary>
            public static Color Yellow => new Color(1.0f, 1.0f, 0.0784313725490196f, 1.0f);
            /// <summary>#75bbfd</summary>
            public static Color SkyBlue => new Color(0.4588235294117647f, 0.7333333333333333f, 0.9921568627450981f, 1.0f);
            /// <summary>#929591</summary>
            public static Color Grey => new Color(0.5725490196078431f, 0.5843137254901961f, 0.5686274509803921f, 1.0f);
            /// <summary>#89fe05</summary>
            public static Color LimeGreen => new Color(0.5372549019607843f, 0.996078431372549f, 0.0196078431372549f, 1.0f);
            /// <summary>#bf77f6</summary>
            public static Color LightPurple => new Color(0.7490196078431373f, 0.4666666666666667f, 0.9647058823529412f, 1.0f);
            /// <summary>#9a0eea</summary>
            public static Color Violet => new Color(0.6039215686274509f, 0.054901960784313725f, 0.9176470588235294f, 1.0f);
            /// <summary>#033500</summary>
            public static Color DarkGreen => new Color(0.011764705882352941f, 0.20784313725490197f, 0.0f, 1.0f);
            /// <summary>#06c2ac</summary>
            public static Color Turquoise => new Color(0.023529411764705882f, 0.7607843137254902f, 0.6745098039215687f, 1.0f);
            /// <summary>#c79fef</summary>
            public static Color Lavender => new Color(0.7803921568627451f, 0.6235294117647059f, 0.9372549019607843f, 1.0f);
            /// <summary>#00035b</summary>
            public static Color DarkBlue => new Color(0.0f, 0.011764705882352941f, 0.3568627450980392f, 1.0f);
            /// <summary>#d1b26f</summary>
            public static Color Tan => new Color(0.8196078431372549f, 0.6980392156862745f, 0.43529411764705883f, 1.0f);
            /// <summary>#00ffff</summary>
            public static Color Cyan => new Color(0.0f, 1.0f, 1.0f, 1.0f);
            /// <summary>#13eac9</summary>
            public static Color Aqua => new Color(0.07450980392156863f, 0.9176470588235294f, 0.788235294117647f, 1.0f);
            /// <summary>#06470c</summary>
            public static Color ForestGreen => new Color(0.023529411764705882f, 0.2784313725490196f, 0.047058823529411764f, 1.0f);
            /// <summary>#ae7181</summary>
            public static Color Mauve => new Color(0.6823529411764706f, 0.44313725490196076f, 0.5058823529411764f, 1.0f);
            /// <summary>#35063e</summary>
            public static Color DarkPurple => new Color(0.20784313725490197f, 0.023529411764705882f, 0.24313725490196078f, 1.0f);
            /// <summary>#01ff07</summary>
            public static Color BrightGreen => new Color(0.00392156862745098f, 0.0f, 0.027450980392156862f, 1.0f);
            /// <summary>#650021</summary>
            public static Color Maroon => new Color(0.396078431372549f, 0.0f, 0.12941176470588237f, 1.0f);
            /// <summary>#6e750e</summary>
            public static Color Olive => new Color(0.43137254901960786f, 0.4588235294117647f, 0.054901960784313725f, 1.0f);
            /// <summary>#ff796c</summary>
            public static Color Salmon => new Color(1.0f, 0.4745098039215686f, 0.4235294117647059f, 1.0f);
            /// <summary>#e6daa6</summary>
            public static Color Beige => new Color(0.9019607843137255f, 0.8549019607843137f, 0.6509803921568628f, 1.0f);
            /// <summary>#0504aa</summary>
            public static Color RoyalBlue => new Color(0.0196078431372549f, 0.01568627450980392f, 0.6666666666666666f, 1.0f);
            /// <summary>#001146</summary>
            public static Color NavyBlue => new Color(0.0f, 0.06666666666666667f, 0.27450980392156865f, 1.0f);
            /// <summary>#cea2fd</summary>
            public static Color Lilac => new Color(0.807843137254902f, 0.6352941176470588f, 0.9921568627450981f, 1.0f);
            /// <summary>#ff028d</summary>
            public static Color HotPink => new Color(1.0f, 0.00784313725490196f, 0.5529411764705883f, 1.0f);
            /// <summary>#ad8150</summary>
            public static Color LightBrown => new Color(0.6784313725490196f, 0.5058823529411764f, 0.3137254901960784f, 1.0f);
            /// <summary>#c7fdb5</summary>
            public static Color PaleGreen => new Color(0.7803921568627451f, 0.9921568627450981f, 0.7098039215686275f, 1.0f);
            /// <summary>#ffb07c</summary>
            public static Color Peach => new Color(1.0f, 0.6901960784313725f, 0.48627450980392156f, 1.0f);
            /// <summary>#677a04</summary>
            public static Color OliveGreen => new Color(0.403921568627451f, 0.47843137254901963f, 0.01568627450980392f, 1.0f);
            /// <summary>#cb416b</summary>
            public static Color DarkPink => new Color(0.796078431372549f, 0.2549019607843137f, 0.4196078431372549f, 1.0f);
            /// <summary>#8e82fe</summary>
            public static Color Periwinkle => new Color(0.5568627450980392f, 0.5098039215686274f, 0.996078431372549f, 1.0f);
            /// <summary>#53fca1</summary>
            public static Color SeaGreen => new Color(0.3254901960784314f, 0.9882352941176471f, 0.6313725490196078f, 1.0f);
            /// <summary>#aaff32</summary>
            public static Color Lime => new Color(0.6666666666666666f, 1.0f, 0.19607843137254902f, 1.0f);
            /// <summary>#380282</summary>
            public static Color Indigo => new Color(0.2196078431372549f, 0.00784313725490196f, 0.5098039215686274f, 1.0f);
            /// <summary>#ceb301</summary>
            public static Color Mustard => new Color(0.807843137254902f, 0.7019607843137254f, 0.00392156862745098f, 1.0f);
            /// <summary>#ffd1df</summary>
            public static Color LightPink => new Color(1.0f, 0.8196078431372549f, 0.8745098039215686f, 1.0f);
        }
    }
}
