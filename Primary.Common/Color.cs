using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Common
{
    public struct Color
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

        public Color32 ToColor32()
        {
            return new Color32(Clamp(R * 255.0f), Clamp(G * 255.0f), Clamp(B * 255.0f), Clamp(A * 255.0f));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static byte Clamp(float v) => (byte)MathF.Min(MathF.Max(v, 0.0f), 255.0f);
        }

        public static Color Black => new Color(0.0f);
        public static Color White => new Color(1.0f);

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
