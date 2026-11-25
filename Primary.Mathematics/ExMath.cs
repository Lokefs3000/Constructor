using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Mathematics
{
    public static class ExMath
    {
        //https://stackoverflow.com/questions/31117497/fastest-integer-square-root-in-the-least-amount-of-instructions
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static uint FastIntergralSqrt(uint val)
        {
            uint temp, g = 0, b = 0x8000;
            int bshft = 15;

            do
            {
                if (val >= (temp = (((g << 1) + b) << bshft--)))
                {
                    g += b;
                    val -= temp;
                }
            } while ((b >>= 1) > 0);

            return g;
        }

        public static float GetAngleTowards(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            return MathF.Atan2(delta.Y, delta.X);
        }
    }
}
