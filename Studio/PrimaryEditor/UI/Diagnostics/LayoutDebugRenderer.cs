using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI;
using EditorUI.Input;
using EditorUI.Layout;
using EditorUI.Visual;
using EditorUI.Widgets;
using Primary.Common;
using Primary.Extensions;
using Primary.Mathematics;
using Primary.Timing;
using PrimaryEditor.Rendering;
using PrimaryEditor.Windows;

namespace PrimaryEditor.UI.Diagnostics
{
    public sealed class LayoutDebugRenderer : IDisposable, ILayoutReporter
    {
        private readonly EditorWindow _editorWindow;
        private readonly Dictionary<Widget, ReportData> _reportingData;

        private bool _disposedValue;

        internal LayoutDebugRenderer(EditorWindow editorWindow)
        {
            UIManager ui = UIManager.Instance;

            _editorWindow = editorWindow;
            _reportingData = new Dictionary<Widget, ReportData>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _reportingData.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Visualize(ref readonly PainterContext painter, Widget widget)
        {
            DrawRecursive(in painter, widget);
            _reportingData.Clear();

            void DrawRecursive(ref readonly PainterContext painter, Widget widget)
            {
                Color color = Color.Maroon;
                Color fill = Color.TransparentBlack;

                if (_reportingData.TryGetValue(widget, out ReportData reportData))
                {
                    color = new Color(reportData.MeasureCount * 0.1f, reportData.LayoutCount * 0.1f, reportData.RelayoutCount * 0.1f);

                    if (reportData.IsGrouped)
                        fill = new Color(Color.SkyBlue) { A = 0.1f };

                    painter.AddRectangle(widget.ComputedRect, new Paint(fill, color, 1));

                    if (reportData.MeasureCount > 0)
                    {
                        Vector2 min = widget.ComputedRect.Minimum + new Vector2(2.0f);
                        painter.AddRectangle(new Boundaries(min, min + new Vector2(2.0f, reportData.MeasureCount * 2.0f)), new Paint(Color.Red));
                    }

                    if (reportData.LayoutCount > 0)
                    {
                        Vector2 min = widget.ComputedRect.Minimum + new Vector2(6.0f, 2.0f);
                        painter.AddRectangle(new Boundaries(min, min + new Vector2(2.0f, reportData.LayoutCount * 2.0f)), new Paint(Color.Green));
                    }

                    if (reportData.RelayoutCount > 0)
                    {
                        Vector2 min = widget.ComputedRect.Minimum + new Vector2(10.0f, 2.0f);
                        painter.AddRectangle(new Boundaries(min, min + new Vector2(2.0f, reportData.RelayoutCount * 2.0f)), new Paint(Color.Blue));
                    }

                    // if (widget.Margin != Vector4.Zero)
                    //     painter.AddRectangle(new Boundaries(widget.ComputedRect.Minimum - widget.Margin.GetLower(), widget.ComputedRect.Maximum + widget.Margin.GetUpper()), new Paint(Color.TransparentBlack, Color.Blue, 1));
                    // if (widget.Padding != Vector4.Zero)
                    //     painter.AddRectangle(new Boundaries(widget.ComputedRect.Minimum + widget.Padding.GetLower(), widget.ComputedRect.Maximum - widget.Padding.GetUpper()), new Paint(Color.TransparentBlack, Color.ForestGreen, 1));
                }

                foreach (Widget child in widget.Children)
                {
                    DrawRecursive(in painter, child);
                }
            }
        }

        public static void VisualizeStatic(ref readonly PainterContext painter, Widget widget)
        {
            DrawRecursive(in painter, widget);

            void DrawRecursive(ref readonly PainterContext painter, Widget widget)
            {
                Color color = Color.Maroon;

                painter.AddRectangle(widget.ComputedRect, new Paint(Color.TransparentBlack, Color.Maroon, 1));

                //if (widget.Margin != Vector4.Zero)
                //    painter.AddRectangle(new Boundaries(widget.ComputedRect.Minimum - widget.Margin.GetLower(), widget.ComputedRect.Maximum + widget.Margin.GetUpper()), new Paint(Color.TransparentBlack, Color.Blue, 1));
                //if (widget.Padding != Vector4.Zero)
                //    painter.AddRectangle(new Boundaries(widget.ComputedRect.Minimum + widget.Padding.GetLower(), widget.ComputedRect.Maximum - widget.Padding.GetUpper()), new Paint(Color.TransparentBlack, Color.ForestGreen, 1));

                foreach (Widget child in widget.Children)
                {
                    DrawRecursive(in painter, child);
                }
            }
        }

        public void OnLayoutBegin()
        {
            _reportingData.Clear();
        }

        public void OnWidgetConsidered(Widget widget, LayoutConsiderType consideredFor)
        {
            ref ReportData reportData = ref CollectionsMarshal.GetValueRefOrAddDefault(_reportingData, widget, out bool exists);
            if (!exists)
                reportData = default;

            if (consideredFor == LayoutConsiderType.Measure)
                ++reportData.MeasureCount;
            else
                ++reportData.LayoutCount;
        }

        public void OnWidgetRelayout(Widget widget)
        {
            ref ReportData reportData = ref CollectionsMarshal.GetValueRefOrAddDefault(_reportingData, widget, out bool exists);
            if (!exists)
                reportData = default;

            ++reportData.RelayoutCount;
        }

        public void OnWidgetGrouped(Widget widget)
        {
            ref ReportData reportData = ref CollectionsMarshal.GetValueRefOrAddDefault(_reportingData, widget, out bool exists);
            if (!exists)
                reportData = default;

            reportData.IsGrouped = true;
        }

        private record struct ReportData(int MeasureCount, int LayoutCount, int RelayoutCount, bool IsGrouped);
    }
}
