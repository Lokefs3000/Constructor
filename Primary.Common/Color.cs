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

        public Vector4 AsVector4() => new Vector4(R, G, B, A);
        public Vector3 AsVector3() => new Vector3(R, G, B);

        public Vector128<float> AsVector128() => Unsafe.ReadUnaligned<Vector128<float>>(ref Unsafe.As<Color, byte>(ref this));

        public Color32 ToColor32()
        {
            Vector128<float> min = Vector128.Create(0.0f, 0.0f, 0.0f, 0.0f);
            Vector128<float> max = Vector128.Create(255.0f, 255.0f, 255.0f, 255.0f);

            Vector128<float> vector = Vector128.Truncate(Vector128.Clamp(AsVector128() * max, min, max));
            Vector128<int> ints = Vector128.ConvertToInt32(vector);

            return new Color32(ints.GetElement(0), ints.GetElement(1), ints.GetElement(2), ints.GetElement(3));
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is Color color && Equals(color);
        public bool Equals(Color color) => Vector128.EqualsAll(AsVector128(), color.AsVector128());

        public override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public override string ToString() => $"<{R},{G},{B},{A}>";

        public static Color Black => new Color(0.0f);
        public static Color White => new Color(1.0f);

        public static Color Red => new Color(1.0f, 0.0f, 0.0f);
        public static Color Green => new Color(0.0f, 1.0f, 0.0f);
        public static Color Blue => new Color(0.0f, 0.0f, 1.0f);
        public static Color Yellow => new Color(1.0f, 1.0f, 0.0f);

        public static Color TransparentWhite => new Color(1.0f, 0.0f);
        public static Color TransparentBlack => new Color(0.0f, 0.0f);

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
    }
}
