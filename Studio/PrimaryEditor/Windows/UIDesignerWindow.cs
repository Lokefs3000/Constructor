using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EditorUI;
using EditorUI.Popup.Menu;
using EditorUI.Reflection;
using EditorUI.Reflection.Cache;
using EditorUI.Serialization;
using EditorUI.Styling;
using EditorUI.Visual;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Assets;
using Primary.Input.Devices;
using Primary.Rendering.Debuggable;
using PrimaryEditor.Assets;
using PrimaryEditor.Core;
using PrimaryEditor.UI.Diagnostics;
using PrimaryEditor.Windows.UIDesigner;

namespace PrimaryEditor.Windows
{
    public sealed class UIDesignerWindow : EditorWindow
    {
        private ViewportManager _viewportManager;

        private LayoutDebugRenderer? _debugRenderer;

        public UIDesignerWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            _viewportManager = new ViewportManager(this);

            LoadLayout("Editor/UI/UIDesigner.layout");
        }

        protected override void DestroySelf()
        {
            _viewportManager.Destroy();
            base.DestroySelf();
        }

        protected internal override void InitializeSelf()
        {
            _viewportManager.Initialize();

            _debugRenderer = new LayoutDebugRenderer(this);
        }

        protected internal override void CleanupReloadSelf()
        {
            _viewportManager.Cleanup();

            _debugRenderer?.Dispose();
            _debugRenderer = null;
        }

        protected override void PaintOverlay(ref readonly PainterContext painter)
        {
            _debugRenderer?.Visualize(in painter, RootWidget);
        }
    }
}
