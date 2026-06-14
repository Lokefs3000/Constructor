using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Dock;
using EditorUI.Serialization;
using EditorUI.Styling;
using EditorUI.Widgets;
using Primary.Mathematics;

namespace EditorUI.Windowing
{
    public abstract class WidgetWindow : WindowBase
    {
        private readonly WindowManager _windowManager;
        
        private Rect _windowRect;

        private DockBase? _parentDock;

        private readonly StylesheetProvider _stylesheetProvider;
        private readonly WindowRoot _rootWidget;

        public WidgetWindow(WindowManager windowManager, ValueSerializer valueSerializer)
        {
            _windowManager = windowManager;

            _windowRect = Rect.Zero;

            _parentDock = null;
     
            _stylesheetProvider = new StylesheetProvider(valueSerializer);
            _rootWidget = new WindowRoot();

            _rootWidget.SetWidgetWindow(this);
        }

        public override bool TryDockInto(DockBase dock)
        {
            // succeed because this is already valid
            if (dock == _parentDock)
                return true;

            dock.TryRemoveWindow(this);

            if (dock.TryAddWindow(this))
            {
                _parentDock = dock;

                TryAddStateFlags(StateFlags.SelfInvalidLayout);
                return true;
            }
            else
            {
                _parentDock = null;
            }

            return false;
        }

        public override bool TryFloat(Int2 targetSize)
        {
            throw new NotImplementedException();
        }

        public override void UpdateData()
        {
        }

        public override void TakeFocus()
        {
            _parentDock?.TryFocusWindow(this);
        }

        public override void Close()
        {
            _windowManager.CloseWindow(this);
        }

        protected internal override void OnFocusGained()
        {
        }

        protected internal override void OnFocusLost()
        {
        }

        protected internal override void RecalculateLayout(Rect windowRect)
        {
            _windowRect = windowRect;
            TryAddStateFlags(StateFlags.SelfInvalidLayout);
        }

        protected internal override void DestroySelf()
        {
            _parentDock?.TryRemoveWindow(this);
            _parentDock = null;

            _rootWidget.Destroy();
        }

        protected internal override void TryAddStateFlags(StateFlags flags) => _rootWidget.AddStateFlags(flags);
        protected internal override void TryRemoveStateFlags(StateFlags flags) => _rootWidget.RemoveStateFlags(flags);

        public override StateFlags StateFlags => _rootWidget.StateFlags;

        public override Rect WindowRect => _windowRect;

        public override DockBase? Parent => _parentDock;

        public override StylesheetProvider StylesheetProvider => _stylesheetProvider;
        public override Widget RootWidget => _rootWidget;
    }
}
