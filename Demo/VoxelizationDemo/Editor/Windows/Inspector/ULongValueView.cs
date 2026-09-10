using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class ULongValueView : IValueView
    {
        public unsafe void ViewValue(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.TargetType == typeof(ulong));

            ulong value = Unsafe.As<OpaqueRef, ulong>(ref inspectorValue.GetValueType());
            if (ImGui.InputScalar("##"u8, ImGuiDataType.U64, &value))
            {
                inspectorValue.SetValueType(value);
            }
        }
    }
}
