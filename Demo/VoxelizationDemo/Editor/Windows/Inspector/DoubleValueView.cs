using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class DoubleValueView : IValueView
    {
        public unsafe void ViewValue(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.TargetType == typeof(double));

            double value = Unsafe.As<OpaqueRef, double>(ref inspectorValue.GetValueType());
            if (ImGui.InputScalar("##"u8, ImGuiDataType.Double, &value))
            {
                inspectorValue.SetValueType(value);
            }
        }
    }
}
