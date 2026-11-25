using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using PrimaryEditor.Core;
using PrimaryEditor.Inspector.Caching;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Windows.Inspector.Populators
{
    // internal class EnumPopulator : WidgetPopulator
    // {
    //     public override void Populate(Widget widget, Type type)
    //     {
    //         EnumValueData enumValueData = EditorRuntime.Instance.InspectorManager.EnumValueCache.GetEnumData(type);
    // 
    //         DropdownField dropdownField = (DropdownField)widget;
    //         dropdownField.ClearOptions();
    // 
    //         foreach (string option in enumValueData.Names)
    //         {
    //             dropdownField.AddOption(option);
    //         }
    //     }
    // 
    //     public override void Update(Widget widget, bool areAllEqual, ROList<IInspectorValue> values)
    //     {
    //         IInspectorValue inspectorValue = values[0];
    //         EnumValueData enumValueData = EditorRuntime.Instance.InspectorManager.EnumValueCache.GetEnumData(inspectorValue.TargetType);
    // 
    //         DropdownField dropdownField = (DropdownField)widget;
    //         if (areAllEqual)
    //         {
    //             ref OpaqueRef value = ref inspectorValue.GetValueType();
    //             int valueIndex = FindValueIndexFor(enumValueData, ref value);
    // 
    //             if (valueIndex == -1)
    //                 dropdownField.Index = 0;
    //             else
    //                 dropdownField.Index = valueIndex;
    //         }
    //         else
    //         {
    //             dropdownField.Index = 0;
    //         }
    //     }
    // 
    //     private static int FindValueIndexFor(EnumValueData enumValueData, ref OpaqueRef value)
    //     {
    //         switch (enumValueData.UnderlyingTypeEnum)
    //         {
    //             case UnderlyingType.Int8:
    //                 {
    //                     sbyte real = Unsafe.As<OpaqueRef, sbyte>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (sbyte)x == real);
    //                 }
    //             case UnderlyingType.Int16:
    //                 {
    //                     short real = Unsafe.As<OpaqueRef, short>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (short)x == real);
    //                 }
    //             case UnderlyingType.Int32:
    //                 {
    //                     int real = Unsafe.As<OpaqueRef, int>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (int)x == real);
    //                 }
    //             case UnderlyingType.Int64:
    //                 {
    //                     long real = Unsafe.As<OpaqueRef, long>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (long)x == real);
    //                 }
    //             case UnderlyingType.UInt8:
    //                 {
    //                     byte real = Unsafe.As<OpaqueRef, byte>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (byte)x == real);
    //                 }
    //             case UnderlyingType.UInt16:
    //                 {
    //                     ushort real = Unsafe.As<OpaqueRef, ushort>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (ushort)x == real);
    //                 }
    //             case UnderlyingType.UInt32:
    //                 {
    //                     uint real = Unsafe.As<OpaqueRef, uint>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (uint)x == real);
    //                 }
    //             case UnderlyingType.UInt64:
    //                 {
    //                     ulong real = Unsafe.As<OpaqueRef, ulong>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (ulong)x == real);
    //                 }
    //             case UnderlyingType.Single:
    //                 {
    //                     float real = Unsafe.As<OpaqueRef, float>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (float)x == real);
    //                 }
    //             case UnderlyingType.Double:
    //                 {
    //                     double real = Unsafe.As<OpaqueRef, double>(ref value);
    //                     return Array.FindIndex(enumValueData.Values, (x) => (double)x == real);
    //                 }
    //         }
    // 
    //         return -1;
    //     }
    // }
}
