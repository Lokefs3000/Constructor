using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector.Views
{
    public interface IValueView
    {
        public void ViewValue(IInspectorValue inspectorValue);
    }
}
