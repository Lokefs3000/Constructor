using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Utility;
using VoxelizationDemo.Editor.Windows.Inspector.Widgets;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class WidgetRelationship
    {
        private readonly List<InspectorWidget> _children;

        internal WidgetRelationship()
        {
            _children = new List<InspectorWidget>();
        }

        internal void Destroy()
        {
            foreach (InspectorWidget child in _children)
            {
                child.DestroySelf();
            }

            _children.Clear();
        }

        internal void AddChild(InspectorWidget child) => _children.AddUnique(child);
        internal void RemoveChild(InspectorWidget child) => _children.Remove(child);

        public ROList<InspectorWidget> Children => _children;
    }
}
