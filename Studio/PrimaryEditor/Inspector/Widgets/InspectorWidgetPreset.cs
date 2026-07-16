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
    public sealed class InspectorWidgetPreset : InspectorWidget
    {
        private readonly Widget _rootWidget;
        private readonly Widget _valuesRootWidget;
        private readonly Label _displayLabel;
        private readonly DropdownField _presetField;
        private readonly LayoutFrame _childWidget;

        internal InspectorWidgetPreset()
        {
            _rootWidget = new Widget()
            {
                Size = UIValue2.MaxX,
                AutoResize = AutoResizeMode.ResizeY
            };
            _valuesRootWidget = new Widget() { Parent = _rootWidget };
            _displayLabel = new Label()
            {
                Parent = _valuesRootWidget,
                Size = new UIValue2(0.5f, 1.0f),
            };
            _presetField = new DropdownField()
            {
                Parent = _valuesRootWidget,
                Position = new UIValue2(0.5f, 0.0f),
                Size = new UIValue2(0.5f, 1.0f)
            };
            _childWidget = new LayoutFrame
            {
                Parent = _rootWidget,
                AutoResize = AutoResizeMode.ResizeY,
            };

            _valuesRootWidget.TryAddClass("field-root");
            _childWidget.TryAddClass("values-root");
            _childWidget.TryAddClass("preset-children");
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

        internal void SetupForDisplay(ViewPreset? viewData)
        {
            base.SetupForDisplay(viewData);

            if (viewData != null)
            {
                _rootWidget.Margin = new Vector4(0.0f, viewData.RelativeOffset.Y, 0.0f, 0.0f);
                _displayLabel.Margin = new Vector4(viewData.RelativeOffset.X, 0.0f, 0.0f, 0.0f);

                _valuesRootWidget.IsEnabled = true;

                _displayLabel.Text = viewData.DisplayName;
                _presetField.ClearOptions();
                foreach (var (presetName, _) in viewData.Presets)
                {
                    _presetField.AddOption(presetName);
                }
            }
            else
            {
                _rootWidget.Margin = Vector4.Zero;
                _displayLabel.Margin = Vector4.Zero;

                _valuesRootWidget.IsEnabled = false;
            }
        }

        public Widget RootWidget => _rootWidget;
        public Widget ChildWidget => _childWidget;
    }
}
