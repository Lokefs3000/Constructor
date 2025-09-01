using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public uint RGBA => ((uint)R << 24) | ((uint)G << 16) | ((uint)B << 8) | ((uint)A);
        public uint ARGB => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | ((uint)B);
    }
}
