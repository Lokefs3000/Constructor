using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using EditorUI.Mathematics;
using EditorUI.Text;
using EditorUI.Widgets;
using Primary.Utility;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Windows.Inspector
{
    internal sealed class InspectorWidgetValue : InspectorWidget
    {
        private readonly InspectorWindow _window;
        private readonly List<IInspectorValue> _values;

        private readonly Widget _rootWidget;
        private readonly Label _valueNameLabel;

        private WidgetPopulator? _widgetPopulator;
        private Widget? _valueWidget;

        internal InspectorWidgetValue(InspectorWindow window)
        {
            _window = window;
            _values = new List<IInspectorValue>();

            _rootWidget = new Widget()
            {
                Size = new UIValue2(1.0f, 0, 0.0f, 24),
                InputState = WidgetInputState.Passthrough,
            };

            _valueNameLabel = new Label()
            {
                Size = new UIValue2(0.35f, 0, 1.0f, 0),
                Alignment = TextAlignment.CenterLeft,
                AllowRichText = false,
                WrapMode = TextWrapMode.Ellipsis,
                InputState = WidgetInputState.Never,

                Parent = _rootWidget
            };

            _widgetPopulator = null;
            _valueWidget = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _valueWidget?.Parent = null;
                    _rootWidget.Destroy();
                }

                _disposedValue = true;
            }
        }

        internal void SetupValueFor(IInspectorValue inspectorValue, Widget? valueWidget)
        {
            _uniqueHash = inspectorValue.UniqueHash;
            _valueNameLabel.Text = inspectorValue.TargetName;

            _valueWidget = valueWidget;

            if (valueWidget != null)
            {
                valueWidget.Position = new UIValue2(1.0f, 0.5f);
                valueWidget.Size = new UIValue2(0.6f, 1.0f);
                valueWidget.Anchor = new Vector2(1.0f, 0.5f);

                valueWidget.Parent = _rootWidget;

                if (_window.PopulatorDatabase.TryGetPopulatorFor(inspectorValue.TargetType, out _widgetPopulator))
                    _widgetPopulator.Populate(valueWidget, inspectorValue.TargetType);
            }
        }

        internal override void AddValueSource(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.UniqueHash == _uniqueHash);

            if (_values.AddUnique(inspectorValue))
            {
                if (_valueWidget != null && _widgetPopulator != null)
                {
                    bool areAllEqual = _values.Count == 1 || _values.All((x) => inspectorValue.Equals(x));
                    _widgetPopulator.Update(_valueWidget, areAllEqual, _values);
                }
            }
        }

        internal override Widget RootWidget => _rootWidget;
    }
}
