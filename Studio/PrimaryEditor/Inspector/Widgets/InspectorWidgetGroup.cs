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
        private readonly Widget _rootWidget;

        internal InspectorWidgetGroup()
        {
            _rootWidget = new Widget()
            {
                Width = UIValue.Max
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
                _rootWidget.Margin = new LayoutBox { Top = viewData.RelativeOffset.Y };
            }
            else
            {
                _rootWidget.Margin = LayoutBox.Null;
            }
        }

        public Widget RootWidget => _rootWidget;
    }
}
