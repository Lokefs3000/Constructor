using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Windows.Inspector.Populators
{
    // internal sealed class BooleanPopulator : WidgetPopulator
    // {
    //     public override void Populate(Widget widget, Type type)
    //     {
    //     }
    // 
    //     public override void Update(Widget widget, bool areAllEqual, ROList<IInspectorValue> values)
    //     {
    //         Checkbox checkbox = (Checkbox)widget;
    //         if (areAllEqual)
    //         {
    //             checkbox.IsChecked = Unsafe.As<OpaqueRef, bool>(ref values[0].GetValueType());
    //         }
    //         else
    //         {
    //             checkbox.IsChecked = false;
    //         }
    //     }
    // }
}
