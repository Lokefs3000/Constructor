using System;
using System.Collections.Generic;
using System.Text;
using Editor.Inspector;
using Primary.Collections.ReadOnly;

namespace Editor.Gui.Inspector
{
    public interface IInspectorView
    {
        public void SetupInspectorElements(ref InspectorElementContext context);
        public void UpdateDisplayedValue(InspectorViewData viewData, InspectorHolder inspectorData, bool hasEquality, int sourceIndex);
        public void SetUpdatedValue(ReadOnlySpan<object> target, InspectorHolder inspectorData, int sourceIndex, string valueString);
    }
}
