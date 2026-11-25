using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using PhysXBindGen.Data;

namespace PhysXBindGen.Emitters
{
    internal static class TypedefEmitter
    {
        public static void Emit(string name, TypedefState typedefState, StringBuilder sb)
        {
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace Interop.PhysX");
            sb.AppendLine("{");
            sb.Append("    public readonly record struct ");
            sb.Append(name.AsSpan(1));
            sb.Append('(');
            sb.Append(EmitterUtil.GetTypeName(typedefState.Datatype));
            sb.AppendLine(" Value)");
            sb.AppendLine("    {");

            sb.Append("        public static implicit operator ");
            sb.Append(name.AsSpan(1));
            sb.Append('(');
            sb.Append(EmitterUtil.GetTypeName(typedefState.Datatype));
            sb.AppendLine(" value) => new(value);");

            sb.Append("        public static implicit operator ");
            sb.Append(EmitterUtil.GetTypeName(typedefState.Datatype));
            sb.Append('(');
            sb.Append(name.AsSpan(1));
            sb.AppendLine(" value) => value.Value;");

            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
    }
}
