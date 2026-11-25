using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Processors.Texture
{
    internal readonly record struct KaiserFilter
    {
        private readonly float _width;
        private readonly float _alpha;
        private readonly float _stretch;

        public KaiserFilter(float width, float alpha, float stretch)
        {
            _width = width;
            _alpha = alpha;
            _stretch = stretch;
        }

        public float Evaluate(float x)
        {
            float sincValue = Sinc(MathF.PI * x * _stretch);
            float t = x / _width;
            if (1.0f - t * t >= 0.0f)
                return sincValue * BesselI0(_alpha * MathF.Sqrt(1.0f - t * t)) / BesselI0(_alpha);
            else
                return 0.0f;
        }

        public float SampleBox(float x, float scale, int samples)
        {
            double sum = 0.0;
            float iSamples = 1.0f / samples;

            for (int s = 0; s < samples; ++s)
            {
                float p = (x + (s + 0.5f) * iSamples) * scale;
                float value = Evaluate(p);

                sum += value;
            }

            return (float)(sum * iSamples);
        }

        private static float Sinc(float x) => x == 0.0f ? 1.0f : MathF.Sin(x) / x;

        private static float BesselI0(float x)
        {
            const float EpsilonRatio = 1e-6f;

            float xh = 0.5f * x;
            float sum = 1.0f;
            float pow = 1.0f;
            float k = 0.0f;
            float ds = 1.0f;

            while (ds > sum * EpsilonRatio)
            {
                ++k;
                pow *= (xh / k);
                ds = pow * pow;
                sum += ds;
            }

            return sum;
        }

        public float Width => _width;
        public float Alpha => _alpha;
        public float Stretch => _stretch;
    }
}
