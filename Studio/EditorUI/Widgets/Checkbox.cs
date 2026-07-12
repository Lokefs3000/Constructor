using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Layout;
using EditorUI.Visual;
using Primary.Common;
using Primary.Input.Devices;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class Checkbox : Button
    {
        protected bool _isChecked;

        protected UIColor _checkmarkColor;
        protected float _checkmarkThickness;

        public Checkbox()
        {
            _isChecked = false;

            _checkmarkColor = Color.Black;
            _checkmarkThickness = 2.0f;
        }

        protected internal override MeasureReturnData MeasureSelf(ref readonly LayoutContext context)
        {
            base.MeasureSelf(in context);

            if (_idealSize.X < _idealSize.Y)
                _idealSize.Y = _idealSize.X;
            else
                _idealSize.X = _idealSize.Y;

            return MeasureReturnData.Success;
        }

        protected internal override void PaintSelf(ref PainterContext context)
        {
            base.PaintSelf(ref context);

            if (_isChecked && _checkmarkThickness >= 1.0f && _checkmarkColor.IsVisible)
            {
                float halfThickness = _checkmarkThickness * 0.5f;
                Vector2 halfSize = _idealSize * 0.5f;

                CheckmarkLineBuffer lineBuffer = new CheckmarkLineBuffer
                {
                    Point0 = _computedRect.Minimum + new Vector2(_idealSize.X - halfThickness - 4.0f, halfThickness + 4.0f),
                    Point1 = _computedRect.Minimum + new Vector2(halfSize.X, _idealSize.Y - halfThickness - 4.0f),
                    Point2 = _computedRect.Minimum + new Vector2(halfThickness + 3.0f, halfSize.Y)
                };

                context.AddLines(MemoryMarshal.CreateReadOnlySpan(ref lineBuffer.Point0, 3), new Paint(_checkmarkColor), LinePaintMode.Strip, _checkmarkThickness);
            }
        }

        public override void HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            if (inputEvent.EventType == UIInputEventType.MousePress && inputEvent.Mouse.Button == MouseButton.Left)
            {
                IsChecked = !IsChecked;
            }

            base.HandleEventSelf(in inputEvent);
        }

        #region Serializable
        [StyleTrigger, Styled(nameof(_isChecked), isEditable: true)]
        public bool IsChecked { get => _isChecked; set => SetEditedField(value); }

        [Styled(nameof(_checkmarkColor))] public UIColor CheckmarkColor { get => _checkmarkColor; set => SetStyledField(value); }
        [Styled(nameof(_checkmarkThickness))] public float CheckmarkThickness { get => _checkmarkThickness; set => SetStyledField(value); }
        #endregion

        private record struct CheckmarkLineBuffer
        {
            public Vector2 Point0;
            public Vector2 Point1;
            public Vector2 Point2;
        }
    }
}
