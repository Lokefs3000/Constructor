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
    internal sealed class InspectorWidgetObject : InspectorWidget
    {
        private readonly List<IInspectorObject> _objects;

        private readonly Widget _rootWidget;
        private readonly LayoutFrame _childWidget;
        private readonly Label _objectNameLabel;

        internal InspectorWidgetObject()
        {
            _objects = new List<IInspectorObject>();

            _rootWidget = new Widget
            {
                Size = new UIValue2(1.0f, 0, 0.0f, 0),
                AutoResize = AutoResizeMode.ResizeY,
            };

            _objectNameLabel = new Label()
            {
                Size = new UIValue2(1.0f, 0, 0.0f, 32),
                Alignment = TextAlignment.CenterLeft,
                AllowRichText = false,
                WrapMode = TextWrapMode.Ellipsis,

                Parent = _rootWidget
            };

            _childWidget = new LayoutFrame
            {
                Position = new UIValue2(0.0f, 0, 0.0f, 32),
                Size = new UIValue2(1.0f, 0, 0.0f, 0),
                AutoResize = AutoResizeMode.ResizeY,

                Padding = new Vector4(24.0f, 0.0f, 0.0f, 0.0f),

                Parent = _rootWidget
            };

            _childWidget.TryAddClass("value-list");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _rootWidget.Destroy();
                }

                _disposedValue = true;
            }
        }

        internal void SetupValueFor(IInspectorObject inspectorObject)
        {
            _uniqueHash = inspectorObject.UniqueHash;
            _objectNameLabel.Text = inspectorObject.TargetName;
        }

        internal override void AddValueSource(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.UniqueHash == _uniqueHash);

            if (_objects.AddUnique((IInspectorObject)inspectorValue))
            {

            }
        }

        internal override Widget RootWidget => _rootWidget;
        internal Widget ChildWidget => _childWidget;
    }
}
