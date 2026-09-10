using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class UIntValueView : IValueView
    {
        public unsafe void ViewValue(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.TargetType == typeof(uint));

            uint value = Unsafe.As<OpaqueRef, uint>(ref inspectorValue.GetValueType());
            if (ImGui.InputScalar("##"u8, ImGuiDataType.U32, &value))
            {
                inspectorValue.SetValueType(value);
            }
        }
    }
}
