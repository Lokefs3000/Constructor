using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Layout;
using EditorUI.Windowing;
using Primary.Mathematics;

namespace EditorUI.Widgets
{
    public sealed class WindowRoot : Widget
    {
        private WidgetWindow? _window;

        internal WindowRoot()
        {
        }

        internal void SetWidgetWindow(WidgetWindow window)
        {
            _window = window;
        }

        protected internal override MeasureStatus MeasureSelf(ref readonly LayoutContext context)
        {
            _layoutState.IdealSize = _window?.WindowRect.Size.AsVector2() ?? Vector2.Zero;
            _layoutState.ContentSize = _layoutState.IdealSize;
            return MeasureStatus.Success;
        }

        protected internal override LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        {
            _computedRect = new Boundaries(Vector2.Zero, _layoutState.IdealSize);
            return LayoutReturnData.Success;
        }

        protected override void ChangeActiveParent(Widget? newParent)
        {
            throw new NotSupportedException("The window root cannot have a parent!");
        }

        public WidgetWindow? OwningWindow => _window;
    }
}
