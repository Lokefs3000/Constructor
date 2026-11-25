using System;
using System.Collections.Generic;
using System.Text;
using PhysXBindGen.Data;

namespace PhysXBindGen.Emitters
{
    internal static class EmitterUtil
    {
        internal static string GetTypeName(TypedefDatatype datatype) => datatype switch
        {
            TypedefDatatype.Int8 => "byte",
            TypedefDatatype.UInt8 => "sbyte",
            TypedefDatatype.Int16 => "short",
            TypedefDatatype.UInt16 => "ushort",
            TypedefDatatype.Int32 => "int",
            TypedefDatatype.UInt32 => "uint",
            TypedefDatatype.Int64 => "long",
            TypedefDatatype.UInt64 => "ulong",
            TypedefDatatype.Single => "float",
            TypedefDatatype.Double => "double",
            TypedefDatatype.Void => "void",
            TypedefDatatype.Bool => "bool",
            _ => throw new NotImplementedException(),
        };

        internal static void WriteTypeName(StringBuilder sb, DatatypeState state, ref string? addendum, int fixedArrayLength = 0)
        {
            if (state is PointerDatatype pointer)
            {
                WriteTypeName(sb, pointer.State, ref addendum, fixedArrayLength);
                sb.Append('*');
            }
            else if (state is FixedArrayDataType fixedArray)
            {
                sb.Append("InlineArray");
                sb.Append(fixedArray.Length);
                sb.Append('<');
                WriteTypeName(sb, fixedArray.State, ref addendum, fixedArrayLength);
                sb.Append('>');
            }
            else
            {
                if (fixedArrayLength > 0)
                    sb.Append("fixed ");

                if (state is StructState structState)
                    sb.Append(structState.Name.AsSpan(1));
                else if (state is ClassState classState)
                    sb.Append(classState.Name);
                else if (state is EnumState enumState)
                    sb.Append(enumState.Name.AsSpan(1));
                else if (state is TypedefState typedefState)
                    sb.Append(typedefState.IsGenericAlias ? GetTypeName(typedefState.Datatype) : typedefState.Name.AsSpan(1));
                else
                    throw new NotSupportedException(state.GetType().Name);
            }
        }
    }
}
