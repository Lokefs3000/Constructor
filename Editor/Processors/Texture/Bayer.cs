using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Processors.Texture
{
    //matricies sourced from: https://github.com/tromero/BayerMatrix/tree/master?tab=readme-ov-file
    internal static class Bayer
    {
        static Bayer()
        {
            for (int i = 0; i < Bayer8x8.Length; i++)
                Bayer8x8[i] = Bayer8x8[i] * 4.0f / 255.0f;
        }

        public static readonly float[] Bayer8x8 = [
            0, 32, 8, 40, 2, 34, 10, 42,
            48, 16, 56, 24, 50, 18, 58, 26,
            12, 44, 4, 36, 14, 46, 6, 38,
            60, 28, 52, 20, 62, 30, 54, 22,
            3, 35, 11, 43, 1, 33, 9, 41,
            51, 19, 59, 27, 49, 17, 57, 25,
            15, 47, 7, 39, 13, 45, 5, 37,
            63, 31, 55, 23, 61, 29, 53, 21
            ];
    }
}
