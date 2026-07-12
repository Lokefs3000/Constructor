using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Serialization;
using EditorUI.Widgets;
using EditorUI.Windowing;
using PrimaryEditor.Windows.LayoutInspector;

namespace PrimaryEditor.Windows
{
    internal sealed class LayoutInspectorWindow : EditorWindow
    {
        private readonly SelectWindowPopup _selectWindowPopup;

        private WidgetWindow? _targetWindow;

        private TreeView? _hierchy;

        public LayoutInspectorWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            _selectWindowPopup = new SelectWindowPopup(this);

            _targetWindow = null;

            LoadLayout("Editor/UI/LayoutInspector.layout");
        }

        protected override void DestroySelf()
        {
            _selectWindowPopup.Dispose();
            base.DestroySelf();
        }

        protected internal override void InitializeSelf()
        {
            _hierchy = RootWidget.FindWidgetWithId<TreeView>("hierchy", true);

            _selectWindowPopup.Initialize();
            _selectWindowPopup.Show();
        }

        protected internal override void CleanupReloadSelf()
        {
            _selectWindowPopup.Cleanup();

            _hierchy = null;
        }

        internal void SetTargetWindow(WidgetWindow window)
        {
            if (_targetWindow != window)
            {
                _targetWindow = window;
            }
        }
    }
}
