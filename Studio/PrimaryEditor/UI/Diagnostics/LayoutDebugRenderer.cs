using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI;
using EditorUI.Input;
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
    public sealed class LayoutDebugRenderer : IDisposable
    {
        private readonly EditorWindow _editorWindow;

        private bool _disposedValue;

        internal LayoutDebugRenderer(EditorWindow editorWindow)
        {
            UIManager ui = UIManager.Instance;

            _editorWindow = editorWindow;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

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

            void DrawRecursive(ref readonly PainterContext painter, Widget widget)
            {
                Color color = Color.Maroon;

                painter.AddRectangle(widget.ComputedRect, new Paint(Color.TransparentBlack, Color.Maroon, 1));

                if (widget.Margin != Vector4.Zero)
                    painter.AddRectangle(new Boundaries(widget.ComputedRect.Minimum - widget.Margin.GetLower(), widget.ComputedRect.Maximum + widget.Margin.GetUpper()), new Paint(Color.TransparentBlack, Color.Blue, 1));
                if (widget.Padding != Vector4.Zero)
                    painter.AddRectangle(new Boundaries(widget.ComputedRect.Minimum + widget.Padding.GetLower(), widget.ComputedRect.Maximum - widget.Padding.GetUpper()), new Paint(Color.TransparentBlack, Color.ForestGreen, 1));

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

                if (widget.Margin != Vector4.Zero)
                    painter.AddRectangle(new Boundaries(widget.ComputedRect.Minimum - widget.Margin.GetLower(), widget.ComputedRect.Maximum + widget.Margin.GetUpper()), new Paint(Color.TransparentBlack, Color.Blue, 1));
                if (widget.Padding != Vector4.Zero)
                    painter.AddRectangle(new Boundaries(widget.ComputedRect.Minimum + widget.Padding.GetLower(), widget.ComputedRect.Maximum - widget.Padding.GetUpper()), new Paint(Color.TransparentBlack, Color.ForestGreen, 1));

                foreach (Widget child in widget.Children)
                {
                    DrawRecursive(in painter, child);
                }
            }
        }
    }
}
