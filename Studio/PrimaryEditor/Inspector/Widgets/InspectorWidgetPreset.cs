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
        private readonly Widget _childWidget;

        internal InspectorWidgetPreset()
        {
            _rootWidget = new Widget()
            {
                Width = UIValue.Max,
            };
            _valuesRootWidget = new Widget() { Parent = _rootWidget };
            _displayLabel = new Label()
            {
                Parent = _valuesRootWidget,
                Width = 0.5f,
                Height = 1.0f
            };
            _presetField = new DropdownField()
            {
                Parent = _valuesRootWidget,
                Left = 0.5f,
                Width = 0.5f,
                Height = 1.0f
            };
            _childWidget = new Widget
            {
                Parent = _rootWidget,
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
                _rootWidget.Margin = new LayoutBox { Top = viewData.RelativeOffset.Y };
                _displayLabel.Margin = new LayoutBox { Left = viewData.RelativeOffset.X };

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
                _rootWidget.Margin = LayoutBox.Null;
                _displayLabel.Margin = LayoutBox.Null;

                _valuesRootWidget.IsEnabled = false;
            }
        }

        public Widget RootWidget => _rootWidget;
        public Widget ChildWidget => _childWidget;
    }
}
