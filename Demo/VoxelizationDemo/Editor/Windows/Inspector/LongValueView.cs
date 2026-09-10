using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class LongValueView : IValueView
    {
        public unsafe void ViewValue(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.TargetType == typeof(long));

            long value = Unsafe.As<OpaqueRef, long>(ref inspectorValue.GetValueType());
            if (ImGui.InputScalar("##"u8, ImGuiDataType.S64, &value))
            {
                inspectorValue.SetValueType(value);
            }
        }
    }
}
