using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI;
using EditorUI.Binding;
using EditorUI.Common;
using EditorUI.Visual;
using EditorUI.Widgets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Timing;
using Primary.Utility;

namespace PrimaryEditor.Windows.GCProfiler
{
    [UIWidget]
    internal sealed class GCGraphView : Widget
    {
        private AverageAnalyser<double> _heapSizeAverage;
        private AverageAnalyser<double> _currentUsageAverage;

        private double _easingTime;

        private UIColor _heapSizeLineColor;
        private UIColor _heapSizeBackgroundColor;

        private UIColor _currentUsageLineColor;
        private UIColor _currentUsageBackgroundColor;

        private double _currentMaximum;

        public GCGraphView()
        {
            _heapSizeAverage = new AverageAnalyser<double>(60, 0.1f);
            _currentUsageAverage = new AverageAnalyser<double>(60, 0.1f);

            _easingTime = 0.5;

            Color heapSizeColor = Color.FromHex("5888bf");
            Color currentUsageColor = Color.FromHex("d581de");

            _heapSizeLineColor = heapSizeColor;
            _heapSizeBackgroundColor = heapSizeColor.Darken(0.5f);

            _currentUsageLineColor = currentUsageColor;
            _currentUsageBackgroundColor = currentUsageColor.Darken(0.5f);

            _currentMaximum = 0.0;
        }

        protected override void PaintSelf(ref PainterContext painter)
        {
            base.PaintSelf(ref painter);

            _heapSizeAverage.Sample(GC.GetGCMemoryInfo().TotalCommittedBytes, Time.DeltaTime);
            _currentUsageAverage.Sample(ProfilingManager.Instance.GCProfiler.CurrentMemoryUsage, Time.DeltaTime);

            double newMaximum = Math.Max(_heapSizeAverage.Max(), _currentUsageAverage.Max());
            if (_currentMaximum < newMaximum || _easingTime <= 0.0)
                _currentMaximum = newMaximum;
            else
                _currentMaximum = double.Lerp(_currentMaximum, newMaximum, Time.DeltaTime * _easingTime);

            double easingSpeed = Time.DeltaTimeDouble * _easingTime;

            float pixelWidth = _computedRect.Maximum.X - _computedRect.Minimum.X;
            double pixelHeight = _computedRect.Maximum.Y - _computedRect.Minimum.Y;

            painter.PushTranslate(_computedRect.Minimum);
            painter.PushClippingRect(new Rect(_computedRect.Size.AsInt2()));

            DrawGraphFor(in painter, ref _heapSizeAverage, _currentMaximum, pixelWidth, pixelHeight, _heapSizeBackgroundColor, _heapSizeLineColor, _computedRect.Maximum.Y);
            DrawGraphFor(in painter, ref _currentUsageAverage, _currentMaximum, pixelWidth, pixelHeight, _currentUsageBackgroundColor, _currentUsageLineColor, _computedRect.Maximum.Y);

            const double MaxTime = 0.1 * 60;
            const double DivTime = 0.1 * 60;

            foreach (TrackedGCMarker marker in ProfilingManager.Instance.GCProfiler.Markers)
            {
                TimeSpan timeDiff = Stopwatch.GetElapsedTime(marker.Timestamp, Time.TimestampForActiveFrame);
                double totalSecs = timeDiff.TotalSeconds;

                if (totalSecs <= MaxTime)
                {
                    float offsetX = (float)((1.0 - totalSecs / DivTime) * pixelWidth);
                    painter.AddCircle(new Vector2(offsetX, 30.0f), 8.0f, new Paint(Color.Yellow));
                }
            }

            painter.PopClippingRect();
            painter.PopTranslate();

            static void DrawGraphFor(ref readonly PainterContext painter, ref AverageAnalyser<double> average, double maximum, float pixelWidth, double pixelHeight, UIColor bg, UIColor fg, float maximumY)
            {
                ReadOnlySpan<double> values = average.Values;

                double valueScale = 1.0 / maximum * pixelHeight;
                if (double.IsInfinity(valueScale))
                    valueScale = 0.0;

                float sampleOffset = MathF.Max(average.Timeout - average.Timer, 0.0f) / average.Timeout - 1.0f;
                float scaleX = 1.0f / (values.Length - 2) * pixelWidth;

                using RentedArray<Vector2> points = RentedArray<Vector2>.Rent(values.Length);

                Paint bgPaint = new Paint(bg);
                Paint fgPaint = new Paint(fg);

                Vector2 lastPoint = default;

                int head = average.Head;
                for (int i = 0; i < values.Length; ++i)
                {
                    if (++head >= values.Length)
                        head = 0;

                    double val = values[head] * valueScale;

                    Vector2 point = new Vector2((i + sampleOffset) * scaleX, (float)(pixelHeight - val));
                    if (i > 0 && bgPaint.Fill.IsVisible)
                    {
                        Vector2 bl = lastPoint;
                        Vector2 br = point;

                        bl.Y = maximumY;
                        br.Y = maximumY;

                        painter.AddQuad(lastPoint, point, bl, br, bgPaint);
                    }

                    points[i] = point;
                    lastPoint = point;
                }

                if (fgPaint.Fill.IsVisible)
                    painter.AddLines(points.Span, fgPaint, LinePaintMode.Strip, 3.0f);
            }
        }

        public double CurrentMaximum => _currentMaximum;
        public double CurrentUsageValue => _currentUsageAverage.Values[_currentUsageAverage.Head];
    }
}
