using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;

namespace Editor.Inspector.Layout
{
    public class LayoutObject
    {
        private readonly InspectorField _field;
        private readonly bool _isObject;
        private string? _propertyName;

        private List<LayoutObject> _children;

        internal LayoutObject(string? propertyName, InspectorField field, bool isObject)
        {
            _field = field;
            _isObject = isObject;
            _propertyName = propertyName;

            _children = new List<LayoutObject>();
        }

        internal void AddChild(LayoutObject @object)
        {
            _children.Add(@object);
        }

        internal void SortChildren(IComparer<LayoutObject> comparer)
        {
            _children.Sort(comparer);
        }

        internal InspectorField Field => _field;
        internal bool IsObject => _isObject;
        internal string? PropertyName { get => _propertyName; set => _propertyName = value; }

        internal ROList<LayoutObject> Children => _children;
    }
}
