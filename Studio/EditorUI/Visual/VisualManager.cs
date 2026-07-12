using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Dock;
using EditorUI.Popup;
using EditorUI.Statistics;
using EditorUI.Utility;
using EditorUI.Visual.Built;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Input;
using Primary.Mathematics;
using Primary.Timing;
using Primary.Windowing;

namespace EditorUI.Visual
{
    public sealed class VisualManager : IDisposable
    {
        private readonly UIManager _manager;

        private GradientManager _gradientManager;

        private List<Painter> _painters;
        private int _painterIndex;

        private List<(Painter Painter, Window Window)> _pendingPaints;

        private bool _disposedValue;

        internal VisualManager(UIManager manager)
        {
            _manager = manager;

            _gradientManager = new GradientManager();

            _painters = new List<Painter>();
            _painterIndex = 0;

            _pendingPaints = new List<(Painter Painter, Window Window)>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (Painter painterData in _painters)
                    {
                        painterData.Dispose();
                    }

                    _painters.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void RenderAll()
        {
            if (_painterIndex > 0)
            {
                for (int i = 0; i < _painterIndex; i++)
                {
                    _painters[i].ClearData();
                }

                _painterIndex = 0;
            }

            _pendingPaints.Clear();

            foreach (DockHost host in _manager.DockManager.DockHosts)
            {
                PaintDockHost(host);
            }

            if (_manager.PopupManager.CurrentPopupHost != null && _manager.PopupManager.PopupWindow != null)
            {
                PaintPopup(_manager.PopupManager.CurrentPopupHost, _manager.PopupManager.PopupWindow);
            }
        }

        private void PaintDockHost(DockHost host)
        {
            VisualStatistics statistics = new VisualStatistics();

            if (_painters.Count <= _painterIndex)
                _painters.Add(new Painter(_gradientManager));

            Painter data = _painters[_painterIndex++];
            data.PushClip(host.HostRect);

            using (new StatTimingScope(ref statistics.GatherCommands))
            {
                {
                    PainterContext painter = new PainterContext(data);
                    host.PaintVisual(in painter);
                }

                if (host.RootDock != null)
                    PaintRecursiveDown(host.RootDock, data);

                #region Debugging
#if false
                {
                    float tick = (float)((Time.TimestampForActiveFrame / (double)Stopwatch.Frequency * 0.2) % 2.0);

                    // background
                    {
                        Vector2 size = host.HostRect.Size.AsVector2() / 10.0f;
                        Int2 abs = (size + new Vector2(0.5f)).AsInt2();

                        Vector2 individualSize = host.HostRect.Size.AsVector2() / size;

                        for (int x = 0; x < abs.X; x++)
                        {
                            for (int y = 0; y < abs.Y; y++)
                            {
                                Vector2 origin = new Vector2(x, y) * individualSize;
                                painter.AddRectangle(new Boundaries(origin, origin + individualSize), new Paint(Color.FromHSV(((x + y) / (float)(abs.X + abs.Y) + tick) * 360.0f, 1.0f, 0.33f)));
                            }
                        }
                    }

                    // points
                    {
                        using RentedArray<Vector2> points = RentedArray<Vector2>.Rent(512);

                        float goldenRatio = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;
                        float goldenAngle = 2.0f * MathF.PI * (1.0f - (1.0f / goldenRatio));

                        Vector2 center = host.HostRect.Size.AsVector2() * 0.5f;

                        for (int i = 0; i < points.Count; i++)
                        {
                            float radius = 15.0f * MathF.Sqrt(i + tick);
                            float theta = i * goldenAngle + (tick * MathF.PI);

                            points[i] = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * radius + center;
                        }

                        painter.AddPoints(points.Span, new Paint(Color.White), 2.0f);
                    }

                    // lines
                    {
                        using RentedArray<Vector2> points = RentedArray<Vector2>.Rent(256);

                        float middle = host.HostRect.Height * 0.5f;
                        float spread = host.HostRect.Width / (float)points.Count * 0.4f;

                        // list
                        {
                            for (int i = 0; i < points.Count; i += 2)
                            {
                                points[i] = new Vector2(i * spread + 24.0f, middle + MathF.Sin((i * 0.1f + tick) * MathF.PI) * 32.0f);
                                points[i + 1] = new Vector2((i + 1) * spread + 24.0f, middle + MathF.Sin(((i + 1) * 0.1f + tick) * MathF.PI) * 32.0f);
                            }

                            painter.AddLines(points.Span, new Paint(Color.Red, Color.NavyBlue, 3), LinePaintMode.List, 8.0f);
                        }

                        // strip
                        {
                            float offset = host.HostRect.Width - 24.0f - points.Count * spread;

                            for (int i = 0; i < points.Count; ++i)
                            {
                                points[i] = new Vector2(offset + i * spread, middle + MathF.Sin((i * 0.1f + tick) * MathF.PI) * 32.0f);
                            }

                            painter.AddLines(points.Span, new Paint(Color.Red, Color.Brown, 3), LinePaintMode.Strip, 8.0f);
                        }

                        painter.AddLine(host.HostRect.Size.AsVector2() * 0.5f, InputSystem.Pointer.MousePosition, new Paint(Color.White, Color.Black, 4), 16.0f);
                    }

                    // rectangle
                    {
                        Vector2 circle = new Vector2(MathF.Sin(tick * MathF.PI * 3.0f), MathF.Cos(tick * MathF.PI * 3.0f)) * 0.5f;

                        Vector4 multiplier = Vector4.Max(Vector4.Zero, new Vector4(
                            Vector2.Distance(circle, new Vector2(-0.5f, -0.5f)),
                            Vector2.Distance(circle, new Vector2(0.5f, -0.5f)),
                            Vector2.Distance(circle, new Vector2(-0.5f, 0.5f)),
                            Vector2.Distance(circle, new Vector2(0.5f, 0.5f))
                            ));

                        ushort strokeWidth = (ushort)(tick * 4.0f);

                        for (int i = 0; i < 8; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                Vector2 origin = new Vector2(i * 100.0f + 30.0f, 30.0f + j * 60.0f);
                                painter.AddRectangle(new Boundaries(origin, origin + new Vector2(80.0f, 40.0f)), new Paint(Color.DefaultColors[j + 30], Color.DefaultColors[j + 13], strokeWidth, (StrokePosition)j), new Vector4(i * 2.0f) * multiplier);
                            }
                        }
                    }

                    // circle
                    {
                        ushort strokeWidth = (ushort)(tick * 4.0f);

                        for (int i = 0; i < 8; i++)
                        {
                            for (int j = 0; j < 3; j++)
                            {
                                Vector2 origin = new Vector2(i * 100.0f + 30.0f, host.HostRect.Height - 130.0f - j * 100.0f);
                                painter.AddCircle(origin + new Vector2(40.0f), i * 5.0f + 5.0f - j * 2.0f, new Paint(Color.DefaultColors[i], Color.White, strokeWidth, (StrokePosition)j));
                            }
                        }
                    }

                    // triangle
                    {
                        ushort strokeWidth = (ushort)(tick * 4.0f);

                        Vector2 center = new Vector2(host.HostRect.Width - 128.0f - 40.0f, 128.0f + 40.0f);

                        for (int i = 0; i < 16; i++)
                        {
                            float time = i / 16.0f * MathF.PI * 2.0f + tick;

                            Vector2 pos = center + new Vector2(MathF.Sin(time), MathF.Cos(time)) * 128.0f;

                            float angle = MathF.Atan2(center.X - pos.X, center.Y - pos.Y);

                            Vector2 forward = new Vector2(MathF.Sin(angle), MathF.Cos(angle)) * 64.0f;
                            Vector2 right = new Vector2(MathF.Sin(angle + MathF.PI * 0.5f), MathF.Cos(angle + MathF.PI * 0.5f)) * 16.0f;

                            Vector2 a = pos - right;
                            Vector2 b = pos + forward;
                            Vector2 c = pos + right;

                            painter.AddTriangle(a, b, c, new Paint(Color.Yellow, Color.DarkGreen, strokeWidth, (StrokePosition)(i % 3)), 8.0f);
                        }

                    }
                }
#endif
                #endregion
            }

            using (new StatTimingScope(ref statistics.PaintBuildTime))
            {
                if (!data.IsEmpty)
                {
                    data.FinishPaint();
                    _pendingPaints.Add((data, host.OwnedWindow));

                    // DrawOutlinesOfCommands(data, host.OwnedWindow);
                }
                else
                {
                    data.ClearData();
                    --_painterIndex;
                }
            }

            host.VisualStatistics = statistics;

            static void PaintRecursiveDown(DockBase dock, Painter painter)
            {
                if (dock.CurrentWindow is WidgetWindow window)
                {
                    painter.PushClip(dock.WindowRect);
                    painter.SetGlobalTranslation(dock.WindowRect.Position.AsVector2());

                    PaintWidgetRecursiveDown(window.RootWidget, painter);

                    PainterContext context = new PainterContext(painter);
                    window.PaintOverlay(in context);

                    painter.PopClip();
                    painter.PopTranslate();
                }

                foreach (DockBase childDock in dock.Docks)
                {
                    PaintRecursiveDown(childDock, painter);
                }
            }

            static void PaintWidgetRecursiveDown(Widget widget, Painter painter)
            {
                int translateStackSize;
                int clipStackSize;

                {
                    PainterContext context = new PainterContext(painter);
                    widget.PaintSelf(ref context);

                    translateStackSize = context.TranslateStackSize;
                    clipStackSize = context.ClipStackSize;
                }

                foreach (Widget child in widget.Children)
                {
                    if (child.IsEnabled)
                        PaintWidgetRecursiveDown(child, painter);
                }

                while (translateStackSize > 0)
                {
                    painter.PopTranslate();
                    --translateStackSize;
                }

                while (clipStackSize > 0)
                {
                    painter.PopClip();
                    --clipStackSize;
                }
            }
        }

        private void PaintPopup(PopupHost popupHost, Window targetWindow)
        {
            if (_painters.Count <= _painterIndex)
                _painters.Add(new Painter(_gradientManager));

            Painter data = _painters[_painterIndex++];
            data.PushClip(new Rect(targetWindow.ClientSize));

            PainterContext painter = new PainterContext(data);
            popupHost.Paint(in painter);

            if (!data.IsEmpty)
            {
                data.FinishPaint();
                _pendingPaints.Add((data, targetWindow));
            }
            else
            {
                data.ClearData();
                --_painterIndex;
            }
        }

        public bool TryGetPaintDataForWindow(Window window, ref int incrementalIndex, [NotNullWhen(true)] out Painter? painter)
        {
            for (; incrementalIndex < _pendingPaints.Count; ++incrementalIndex)
            {
                if (window == _pendingPaints[incrementalIndex].Window)
                {
                    painter = _pendingPaints[incrementalIndex].Painter;
                    ++incrementalIndex;
                    return true;
                }
            }

            painter = null;
            return false;
        }

        private void DrawOutlinesOfCommands(Painter target, Window window)
        {
            if (target.IsEmpty)
                return;

            if (_painters.Count <= _painterIndex)
                _painters.Add(new Painter(_gradientManager));

            Painter data = _painters[_painterIndex++];
            data.PushClip(new Rect(window.ClientSize));

            Paint paint = new Paint(Color.TransparentBlack, Color.Maroon, 1, StrokePosition.Inside);

            foreach (PaintCmd cmd in target.Commands)
            {
                data.AddRectangle(cmd.RenderBounds, paint, Vector4.NegativeZero);
            }

            data.FinishPaint();
            _pendingPaints.Add((data, window));
        }

        public GradientManager GradientManager => _gradientManager;

        public ROList<(Painter Painter, Window Window)> PendingPaints => _pendingPaints;

        public bool HasPendingPaints => _pendingPaints.Count > 0;
    }
}
