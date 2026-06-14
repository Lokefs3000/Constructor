using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Editor.Inspector;
using Editor.UI.Elements;

namespace Editor.Gui.Inspector.Views
{
    [InspectorViewTarget(typeof(Vector2))]
    internal class Vector2InspectorView : IInspectorView
    {
        private int _xIndex;
        private int _yIndex;

        public void SetupInspectorElements(ref InspectorElementContext context)
        {
            _xIndex = context.AddField<UITextField>(0.5f);
            _yIndex = context.AddField<UITextField>(0.5f);
        }

        public void UpdateDisplayedValue(InspectorViewData viewData, InspectorHolder inspectorData, bool hasEquality, int sourceIndex)
        {
            InspectorObject<Vector2> value = inspectorData.GetValueAt<Vector2>(sourceIndex);

            UITextField xField = viewData.GetElementAt<UITextField>(_xIndex);
            UITextField yField = viewData.GetElementAt<UITextField>(_yIndex);

            if (hasEquality)
            {
                xField.Text = value.Value.X.ToString();
                yField.Text = value.Value.Y.ToString();
            }
            else
            {
                xField.Text = "-";
                yField.Text = "-";
            }
        }

        public void SetUpdatedValue(ReadOnlySpan<object> target, InspectorHolder inspectorData, int sourceIndex, string valueString)
        {
            if (byte.TryParse(valueString, out byte result))
            {
                inspectorData.SetValues(target, result);
            }
        }
    }
}
