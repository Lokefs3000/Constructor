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
                Size = new UIValue2(0.5f, 1.0f),
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
                _rootWidget.Margin = new Vector4(0.0f, viewData.RelativeOffset.Y, 0.0f, 0.0f);
                _displayLabel.Margin = new Vector4(viewData.RelativeOffset.X, 0.0f, 0.0f, 0.0f);

                _displayLabel.Text = viewData.DisplayName;
            }
            else
            {
                _rootWidget.Margin = Vector4.Zero;
                _displayLabel.Margin = Vector4.Zero;

                _displayLabel.Text = null;
            }
        }

        public Widget RootWidget => _rootWidget;
    }
}
