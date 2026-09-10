using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Hexa.NET.ImGui;
using PrimaryEditor.Inspector.Values;

namespace VoxelizationDemo.Editor.Windows.Inspector
{
    internal sealed class StringValueView : IValueView
    {
        public void ViewValue(IInspectorValue inspectorValue)
        {
            Debug.Assert(inspectorValue.TargetType == typeof(string));

            string value = (string?)inspectorValue.GetObjectValue() ?? string.Empty;
            if (ImGui.InputText("##"u8, ref value, nuint.MaxValue))
            {
                inspectorValue.SetObject(value);
            }
        }
    }
}
