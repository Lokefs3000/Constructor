using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Serialization;
using EditorUI.Visual;
using EditorUI.Windowing;
using PrimaryEditor.UI.Diagnostics;
using PrimaryEditor.Windows.GCProfiler;

namespace PrimaryEditor.Windows
{
    public sealed class GCProfilerWindow : EditorWindow
    {
        private LayoutDebugRenderer? _debugRenderer;

        private GraphContentView _graphContentView;

        public GCProfilerWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            LoadLayout("Editor/UI/GCProfiler.layout");

            _graphContentView = new GraphContentView(this);
        }

        protected override void DestroySelf()
        {
            _debugRenderer?.Dispose();
            _debugRenderer = null;

            base.DestroySelf();
        }

        protected internal override void InitializeSelf()
        {
            _debugRenderer ??= new LayoutDebugRenderer(this);

            _graphContentView.Initialize();
        }

        protected internal override void CleanupReloadSelf()
        {
            _graphContentView.Cleanup();

            _debugRenderer?.Dispose();
            _debugRenderer = null;
        }

        public override void UpdateData()
        {
            _graphContentView.Update();
        }
    }
}
