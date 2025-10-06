using System.Globalization;
using System.Numerics;

namespace Primary.Common
{
    public record struct Color32
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Color32(byte scalar, byte a = 255)
        {
            R = scalar;
            G = scalar;
            B = scalar;
            A = a;
        }

        public Color32(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public Color32(float r, float g, float b, float a = 1.0f)
        {
            R = (byte)Math.Clamp((int)(r * 255.0f), 0, 255);
            G = (byte)Math.Clamp((int)(g * 255.0f), 0, 255);
            B = (byte)Math.Clamp((int)(b * 255.0f), 0, 255);
            A = (byte)Math.Clamp((int)(a * 255.0f), 0, 255);
        }

        public Color32(uint rgba, bool asArgb = false)
        {
            if (asArgb)
            {
                A = (byte)((rgba >> 24) & 0xff);
                R = (byte)((rgba >> 16) & 0xff);
                G = (byte)((rgba >> 8) & 0xff);
                B = (byte)((rgba) & 0xff);
            }
            else
            {
                R = (byte)((rgba >> 24) & 0xff);
                G = (byte)((rgba >> 16) & 0xff);
                B = (byte)((rgba >> 8) & 0xff);
                A = (byte)((rgba) & 0xff);
            }
        }

        public Color32(Vector3 rgb, float a = 1.0f)
        {
            Vector4 clamped = Vector4.Clamp(Vector4.Round(new Vector4(rgb, a) * 255.0f), Vector4.Zero, new Vector4(255.0f));

            R = (byte)clamped.X;
            G = (byte)clamped.Y;
            B = (byte)clamped.Z;
            A = (byte)clamped.W;
        }

        public Color32(Vector4 rgba)
        {
            Vector4 clamped = Vector4.Clamp(Vector4.Round(rgba * 255.0f), Vector4.Zero, new Vector4(255.0f));

            R = (byte)clamped.X;
            G = (byte)clamped.Y;
            B = (byte)clamped.Z;
            A = (byte)clamped.W;
        }

        public uint RGBA => ((uint)R << 24) | ((uint)G << 16) | ((uint)B << 8) | ((uint)A);
        public uint BGRA => ((uint)B << 24) | ((uint)G << 16) | ((uint)R << 8) | ((uint)A);
        public uint ARGB => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | ((uint)B);
        public uint ABGR => ((uint)A << 24) | ((uint)B << 16) | ((uint)G << 8) | ((uint)R);

        public static Color32 FromHex(ReadOnlySpan<char> hex)
        {
            uint rgba = uint.Parse(hex, NumberStyles.HexNumber);
            return new Color32(rgba);
        }
    }
}
