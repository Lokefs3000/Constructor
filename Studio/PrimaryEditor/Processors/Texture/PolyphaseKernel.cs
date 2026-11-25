using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;

namespace PrimaryEditor.Processors.Texture
{
    internal readonly record struct PolyphaseKernel : IDisposable
    {
        private readonly int _windowSize;
        private readonly int _length;
        private readonly float _width;
        private readonly RentedArray<float> _data;

        public PolyphaseKernel(in KaiserFilter filter, int srcLength, int dstLength, int samples = 32)
        {
            float scale = dstLength / (float)srcLength;
            float iScale = 1.0f / scale;

            if (scale > 1)
            {
                samples = 1;
                scale = 1.0f;
            }

            _length = dstLength;
            _width = filter.Width * iScale;
            _windowSize = (int)MathF.Ceiling(_width * 2.0f) + 1;

            _data = new RentedArray<float>(_windowSize * _length);
            _data.Span.Fill(0.0f);

            for (int i = 0; i < _length; ++i)
            {
                float center = (i + 0.5f) * iScale;

                int left = (int)MathF.Floor(center - _width);
                int right = (int)MathF.Ceiling(center + _width);

                float total = 0.0f;
                for (int j = 0; j < _windowSize; ++j)
                {
                    float sample = filter.SampleBox(left + j - center, scale, samples);

                    _data[i * _windowSize + j] = sample;
                    total += sample;
                }

                for (int j = 0; j < _windowSize; ++j)
                {
                    _data[i * _windowSize + j] /= total;
                }
            }
        }

        public void Dispose()
        {
            _data.Dispose();
        }

        public float this[int column, int x] => _data[column * _windowSize + x];

        public int WindowSize => _windowSize;
        public int Length => _length;
        public float Width => _width;
    }
}
