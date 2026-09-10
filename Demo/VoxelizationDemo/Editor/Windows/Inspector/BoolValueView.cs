using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class BoolValueView : IValueView
    {
        public void ViewValue(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.TargetType == typeof(bool));

            bool value = Unsafe.As<OpaqueRef, bool>(ref inspectorValue.GetValueType());
            if (ImGui.Checkbox("##"u8, ref value))
            {
                inspectorValue.SetValueType(value);
            }
        }
    }
}
