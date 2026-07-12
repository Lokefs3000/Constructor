using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Windows.Inspector
{
    internal abstract class WidgetPopulator
    {
        public abstract void Populate(Widget widget, Type type);
        public abstract void Update(Widget widget, bool areAllEqual, ROList<IInspectorValue> values);
    }
}
