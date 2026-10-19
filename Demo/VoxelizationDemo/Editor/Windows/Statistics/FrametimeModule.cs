using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Timing;

namespace VoxelizationDemo.Editor.Windows.Statistics
{
    internal sealed class FrametimeModule : IStatisticsModule
    {
        private float[] _frametimes;

        private float _frametimeTimeout;
        private int _frametimeIndex;

        internal FrametimeModule()
        {
            _frametimes = new float[10 * 60]; // 10 seconds with 60 fps
        }

        public void ShowStatistics()
        {
            (float frametimeMin, float frametimeMax, float frametimeAvg) = CalculateFrametimes();
            float smoothness = 1.0f - (((frametimeMax - frametimeMin) / 4.0f) / frametimeAvg);

            ImGui.TextUnformatted($"Frametime raw: {Time.DeltaTime * 1000.0f:f3}ms ({1.0f / Time.DeltaTime:f1} fps)");
            ImGui.TextUnformatted($"Frametime average: {frametimeAvg * 1000.0f:f3}ms ({1.0f / frametimeAvg:f1} fps)");
            ImGui.TextUnformatted($"Frametime smoothness: {smoothness * 100.0f:f1}%");
            ImGui.TextUnformatted($"Frametime min: {frametimeMin * 1000.0f:f3}ms ({1.0f / frametimeMin:f1} fps)");
            ImGui.TextUnformatted($"Frametime max: {frametimeMax * 1000.0f:f3}ms ({1.0f / frametimeMax:f1} fps)");

            _frametimes[_frametimeIndex = ++_frametimeIndex % _frametimes.Length] = Time.DeltaTime;
        }

        private (float FrametimeMin, float FrametimeMax, float FrametimeAverage) CalculateFrametimes()
        {
            float min = 100.0f;
            float max = 0.0f;
            float avg = 0.0f;

            if (_frametimes.Length >= Vector256<float>.Count)
            {
                Vector256<float> simdMin = Vector256.Create(100.0f);
                Vector256<float> simdMax = Vector256<float>.Zero;
                Vector256<float> simdSum = Vector256<float>.Zero;

                for (int i = 0; i < _frametimes.Length; i += Vector256<float>.Count)
                {
                    Vector256<float> vector = Vector256.LoadUnsafe(ref _frametimes[i]);
                    simdMin = Vector256.Min(simdMin, vector);
                    simdMax = Vector256.Max(simdMax, vector);
                    simdSum = Vector256.Add(simdSum, vector);
                }

                Vector128<float> narrowMin = Vector128.Min(simdMin.GetLower(), simdMin.GetUpper());
                Vector128<float> narrowMax = Vector128.Max(simdMax.GetLower(), simdMax.GetUpper());

                min = MathF.Min(MathF.Min(narrowMin[0], narrowMin[1]), MathF.Min(narrowMin[2], narrowMin[3]));
                max = MathF.Min(MathF.Min(narrowMax[0], narrowMax[1]), MathF.Min(narrowMax[2], narrowMax[3]));
                avg = Vector256.Sum(simdSum);
            }

            for (int i = (_frametimes.Length / Vector256<float>.Count) * Vector256<float>.Count; i < _frametimes.Length; ++i)
            {
                ref float value = ref _frametimes[i];

                min = MathF.Min(min, value);
                max = MathF.Max(max, value);
                avg += value;
            }

            return (min, max, (float)((double)avg / (double)_frametimes.Length));
        }
    }
}
