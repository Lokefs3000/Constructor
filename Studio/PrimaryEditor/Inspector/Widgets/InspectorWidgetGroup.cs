using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views;
using TerraFX.Interop.Windows;

namespace PrimaryEditor.Inspector.Widgets
{
    public sealed class InspectorWidgetGroup : InspectorWidget
    {
        private readonly LayoutFrame _rootWidget;

        internal InspectorWidgetGroup()
        {
            _rootWidget = new LayoutFrame()
            {
                Size = UIValue2.MaxX,
                AutoResize = AutoResizeMode.ResizeY
            };

            _rootWidget.TryAddClass("values-root");
        }

        internal override void DestroySelf()
        {
            _rootWidget.Destroy();
        }

        internal override void ClearForPooling()
        {
            base.ClearForPooling();

            _rootWidget.Parent = null;
        }

        internal void SetupForDisplay(ViewGroup? viewData)
        {
            base.SetupForDisplay(viewData);

            if (viewData != null)
            {
                _rootWidget.Margin = new Vector4(0.0f, viewData.RelativeOffset.Y, 0.0f, 0.0f);
            }
            else
            {
                _rootWidget.Margin = Vector4.Zero;
            }
        }

        public Widget RootWidget => _rootWidget;
    }
}
