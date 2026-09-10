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
    public sealed class InspectorWidgetField : InspectorWidget
    {
        private readonly Widget _rootWidget;
        private readonly Label _displayLabel;

        internal InspectorWidgetField()
        {
            _rootWidget = new Widget();
            _displayLabel = new Label()
            {
                Parent = _rootWidget,
                Width = 0.5f,
                Height = UIValue.Max
            };

            _rootWidget.TryAddClass("field-root");
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

        internal void SetupForDisplay(ViewField? viewData)
        {
            base.SetupForDisplay(viewData);

            if (viewData != null)
            {
                _rootWidget.Margin = new LayoutBox { Top = viewData.RelativeOffset.Y };
                _displayLabel.Margin = new LayoutBox { Left = viewData.RelativeOffset.X };

                _displayLabel.Text = viewData.DisplayName;
            }
            else
            {
                _rootWidget.Margin = LayoutBox.Null;
                _displayLabel.Margin = LayoutBox.Null;

                _displayLabel.Text = null;
            }
        }

        public Widget RootWidget => _rootWidget;
    }
}
