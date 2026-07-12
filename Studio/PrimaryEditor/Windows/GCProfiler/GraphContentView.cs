using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Widgets;
using Primary.Common;

namespace PrimaryEditor.Windows.GCProfiler
{
    internal sealed class GraphContentView
    {
        private readonly GCProfilerWindow _window;

        private Widget? _contentWidget;
        private LayoutFrame? _timingLabels;
        private Widget? _memoryLabels;
        private GCGraphView? _primaryGraph;

        private double _maximumLastTime;

        internal GraphContentView(GCProfilerWindow window)
        {
            _window = window;
        }

        internal void Initialize()
        {
            _contentWidget = _window.RootWidget.FindWidgetWithId<Widget>("graph");

            if (_contentWidget != null)
            {
                _timingLabels = _contentWidget.FindWidgetWithId<LayoutFrame>("timing");
                _memoryLabels = _contentWidget.FindWidgetWithId<Widget>("memory");
                _primaryGraph = _contentWidget.FindWidgetWithId<GCGraphView>("graph");
            }
        }

        internal void Cleanup()
        {
            _contentWidget = null;
            _timingLabels = null;
            _memoryLabels = null;
            _primaryGraph = null;

            _maximumLastTime = 0.0;
        }

        internal void Update()
        {
            if (_memoryLabels != null && _primaryGraph != null)
            {
                if (_maximumLastTime != _primaryGraph.CurrentMaximum)
                {
                    Label maxLabel = (Label)_memoryLabels.Children[0];
                    maxLabel.Text = FileUtility.FormatSize((long)_primaryGraph.CurrentMaximum, "F1");
                    
                    _maximumLastTime = _primaryGraph.CurrentMaximum;
                }

                float verticalPosition = (float)((1.0 - _primaryGraph.CurrentUsageValue / _primaryGraph.CurrentMaximum) * _memoryLabels.IdealSize.Y);
                Label currentLabel = (Label)_memoryLabels.Children[2];

                currentLabel.Text = FileUtility.FormatSize((long)_primaryGraph.CurrentUsageValue, "F1");
                currentLabel.Position = new UIValue2(0, Math.Clamp((int)verticalPosition, (int)currentLabel.IdealSize.Y, (int)(_memoryLabels.IdealSize.Y - currentLabel.IdealSize.Y)));
            }
        }
    }
}
