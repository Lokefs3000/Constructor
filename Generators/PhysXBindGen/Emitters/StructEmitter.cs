using System.Collections.Frozen;
using System.Text;
using PhysXBindGen.Data;

namespace PhysXBindGen.Emitters
{
    internal static class StructEmitter
    {
        public static void Emit(StructState structState, StringBuilder sb)
        {
            List<StructMember> members = structState.Members;

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.InteropServices;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine();
            sb.AppendLine("namespace Interop.PhysX");
            sb.AppendLine("{");
            if (structState.TreatAsUnion)
            {
                sb.AppendLine("    [StructLayout(LayoutKind.Explicit)]");
            }
            sb.Append("    public unsafe struct ");
            sb.Append(structState.Name.AsSpan(1));
            sb.AppendLine();
            sb.AppendLine("    {");

            for (int i = 0; i < members.Count; ++i)
            {
                StructMember member = members[i];
                string? addendum = null;

                if (structState.TreatAsUnion)
                {
                    sb.AppendLine("        [FieldOffset(0)]");
                }

                sb.Append("        public ");
                EmitterUtil.WriteTypeName(sb, member.Datatype, ref addendum);
                sb.Append(' ');
                if (s_reservedNames.Contains(member.Name))
                    sb.Append('@');
                sb.Append(member.Name);
                if (addendum != null)
                    sb.Append(addendum);
                sb.AppendLine(";");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        private static readonly FrozenSet<string> s_reservedNames = new HashSet<string>
        {
            "base",
            "this",
            "class",
            "struct",
            "enum"
        }.ToFrozenSet();
    }
}
