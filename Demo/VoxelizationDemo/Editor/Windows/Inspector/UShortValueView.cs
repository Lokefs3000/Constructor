using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class UShortValueView : IValueView
    {
        public void ViewValue(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.TargetType == typeof(ushort));

            int value = Unsafe.As<OpaqueRef, ushort>(ref inspectorValue.GetValueType());
            if (ImGui.InputInt("##"u8, ref value))
            {
                inspectorValue.SetValueType((ushort)value);
            }
        }
    }
}
