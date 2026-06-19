using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Dock;
using EditorUI.Visual.Built;
using EditorUI.Visual.Draw;
using EditorUI.Visual.Passes;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;

namespace EditorUI.Visual
{
    public sealed class VisualManager : IDisposable
    {
        private readonly UIManager _manager;

        private GradientManager _gradientManager;

        private List<PainterData> _painterDatas;
        private List<BuiltPaintData> _builtPaints;
        private int _painterDataIndex;

        private bool _disposedValue;

        internal VisualManager(UIManager manager)
        {
            _manager = manager;

            _gradientManager = new GradientManager();

            _painterDatas = new List<PainterData>();
            _builtPaints = new List<BuiltPaintData>();
            _painterDataIndex = 0;

            Engine.GlobalSingleton.RenderingManager.RenderPassManager.AddRenderPass<RenderUIPass>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (PainterData painterData in _painterDatas)
                    {
                        painterData.Dispose();
                    }

                    foreach (BuiltPaintData paintData in _builtPaints)
                    {
                        paintData.Dispose();
                    }

                    _painterDatas.Clear();

                    Engine.GlobalSingleton.RenderingManager.RenderPassManager.RemoveRenderPass<RenderUIPass>();
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
            if (_painterDataIndex > 0)
            {
                for (int i = 0; i < _painterDataIndex; i++)
                {
                    _painterDatas[i].ClearForNewPaint();
                    _builtPaints[i].ClearInternalData();
                }

                _painterDataIndex = 0;
            }

            foreach (DockHost host in _manager.DockManager.DockHosts)
            {
                PaintDockHost(host);
            }
        }

        private void PaintDockHost(DockHost host)
        {
            if (_painterDatas.Count <= _painterDataIndex)
                _painterDatas.Add(new PainterData());

            PainterData data = _painterDatas[_painterDataIndex++];

            PainterContext painter = new PainterContext(data, _gradientManager);
            //host.PaintVisual(in painter);

            //if (host.RootDock != null)
            //    PaintRecursiveDown(host.RootDock, in painter);

            painter.AddRectangle(new Boundaries(Vector2.Zero, new Vector2(100.0f)), new Paint(Color.Green, Color.Blue, 8));

            if (!data.IsEmpty)
            {
                if (_builtPaints.Count < _painterDataIndex)
                    _builtPaints.Add(new BuiltPaintData());

                BuiltPaintData builtData = _builtPaints[_painterDataIndex - 1];
                builtData.BuildRenderingData(host.OwnedWindow, data);
            }
            else
            {
                data.ClearForNewPaint();
                --_painterDataIndex;
            }

            static void PaintRecursiveDown(DockBase dock, ref readonly PainterContext painter)
            {
                if (dock.CurrentWindow is WidgetWindow window)
                {
                    PaintWidgetRecursiveDown(window.RootWidget, in painter);
                }

                foreach (DockBase childDock in dock.Docks)
                {
                    PaintRecursiveDown(childDock, in painter);
                }
            }

            static void PaintWidgetRecursiveDown(Widget widget, ref readonly PainterContext painter)
            {
                widget.PaintSelf(in painter);

                foreach (Widget child in widget.Children)
                {
                    PaintWidgetRecursiveDown(child, in painter);
                }
            }
        }

        public ReadOnlySpan<BuiltPaintData> BuiltPaints => _builtPaints.AsSpan()[.._painterDataIndex];
    }
}
