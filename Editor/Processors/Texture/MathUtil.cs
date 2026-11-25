using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.Processors.Texture
{
    // sourced from: "MathNet.Numerics.SpecialFunctions.BesselI0"
    internal static class MathUtil
    {
        public static float BesselI0(float x)
        {
            if (x < 0.0f)
                x = -x;

            if (x <= 8.0f)
            {
                float x2 = x * 0.5f - 2.0f;
                return MathF.Exp(x) * ChebyshevA(BesselI0B, x2);
            }

            float x3 = 32.0f / x - 2.0f;
            return MathF.Exp(x) * ChebyshevA(BesselI0B, x3) / MathF.Sqrt(x);
        }

        public static float ChebyshevA(float[] coefficients, float x)
        {
            int num = 1;
            float num2 = coefficients[0];
            float num3 = 0.0f;
            int num4 = coefficients.Length - 1;
            float num5;
            do
            {
                num5 = num3;
                num3 = num2;
                num2 = x * num3 - num5 + coefficients[num++];
            } while (--num4 > 0);

            return 0.5f * (num2 - num5);
        }

        private static readonly float[] BesselI0B =
        [
            -7.2331804878747538E-18f, -4.8305044859441819E-18f, 4.46562142029676E-17f, 3.4612228676974612E-17f, -2.8276239805165836E-16f, -3.425485619677219E-16f, 1.7725601330565263E-15f, 3.8116806693526224E-15f, -9.5548466988283073E-15f, -4.1505693472872222E-14f,
            1.54008621752141E-14f, 3.8527783827421426E-13f, 7.180124451383666E-13f, -1.7941785315068062E-12f, -1.3215811840447713E-11f, -3.1499165279632416E-11f, 1.1889147107846439E-11f, 4.94060238822497E-10f, 3.3962320257083865E-09f, 2.266668990498178E-08f,
            2.0489185894690638E-07f, 2.8913705208347567E-06f, 6.8897583469168245E-05f, 0.0033691164782556943f, 0.80449041101410879f
        ];
    }
}
