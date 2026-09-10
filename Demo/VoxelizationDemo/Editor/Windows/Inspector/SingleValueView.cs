using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class SingleValueView : IValueView
    {
        public unsafe void ViewValue(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.TargetType == typeof(float));

            float value = Unsafe.As<OpaqueRef, float>(ref inspectorValue.GetValueType());
            if (ImGui.InputScalar("##"u8, ImGuiDataType.Float, &value))
            {
                inspectorValue.SetValueType(value);
            }
        }
    }
}
