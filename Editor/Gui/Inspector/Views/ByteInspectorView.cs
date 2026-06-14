using System;
using System.Collections.Generic;
using System.Text;
using Editor.Inspector;
using Editor.UI.Datatypes;
using Editor.UI.Elements;

namespace Editor.Gui.Inspector.Views
{
    [InspectorViewTarget(typeof(byte))]
    internal class ByteInspectorView : IInspectorView
    {
        private int _fieldIndex;

        public void SetupInspectorElements(ref InspectorElementContext context)
        {
            _fieldIndex = context.AddField<UITextField>();
        }

        public void UpdateDisplayedValue(InspectorViewData viewData, InspectorHolder inspectorData, bool hasEquality, int sourceIndex)
        {
            InspectorObject<byte> value = inspectorData.GetValueAt<byte>(sourceIndex);

            UITextField field = viewData.GetElementAt<UITextField>(_fieldIndex);

            if (hasEquality)
            {
                field.Text = value.Value.ToString();
            }
            else
            {
                field.Text = "-";
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
