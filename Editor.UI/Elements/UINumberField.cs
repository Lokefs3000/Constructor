using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("NumberField")]
    public class UINumberField : UITextField
    {
        private double _value;

        private double _maxValue;
        private double _minValue;

        public UINumberField()
        {
            _value = 0.0;

            _maxValue = double.MaxValue;
            _minValue = double.MinValue;

            SetNewText(_value.ToString(CultureInfo.InvariantCulture));
        }

        public UINumberField(UIElement parent) : this()
        {
            SetParent(parent);
        }

        protected override bool OnTextCommited(ref string text)
        {
            if (double.TryParse(text, CultureInfo.InvariantCulture, out double result))
            {
                result = Math.Clamp(result, _minValue, _maxValue);
                text = result.ToString(CultureInfo.InvariantCulture);

                OnValueChanged?.Invoke(result);
                return true;
            }

            text = result.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        protected void SetNewValue(double value)
        {
            _value = Math.Clamp(value, _minValue, _maxValue);
            OnValueChanged?.Invoke(value);
            SetNewText(value.ToString(CultureInfo.InvariantCulture));
        }

        #region Properties
        [EditableProperty("", UIStateFlags.InvalidVisual)] public double Value { get => _value; set => SetNewValue(value); }
        [EditableProperty(nameof(_minValue))] public double Min { get => _value; set => _minValue = value; }
        [EditableProperty(nameof(_maxValue))] public double Max { get => _value; set => _maxValue = value; }
        #endregion
        #region Events
        public event Action<double>? OnValueChanged;
        #endregion
    }
}
