using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Primary.Collections;
using PrimaryEditor.Inspector.Values;
using PrimaryEditor.Inspector.Views;

namespace PrimaryEditor.Inspector.Widgets
{
    public record struct WidgetCondition
    {
        private readonly ViewCondition _view;
        private RentedList<IInspectorValue> _values;

        internal WidgetCondition(ViewCondition view, RentedList<IInspectorValue> values)
        {
            _view = view;
            _values = values;
        }

        public readonly ViewCondition View => _view;
        [UnscopedRef]
        public ref RentedList<IInspectorValue> Values => ref _values;
    }
}
