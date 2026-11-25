using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using PhysXBindGen.Data;

namespace PhysXBindGen.Emitters
{
    internal static class EnumEmitter
    {
        public static void Emit(EnumState enumState, StringBuilder sb)
        {
            List<EnumValue> enumValues = enumState.Values;

            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace Interop.PhysX");
            sb.AppendLine("{");
            sb.Append("    public enum ");
            sb.Append(enumState.Name.AsSpan(1));
            sb.Append(" : ");
            sb.AppendLine(EmitterUtil.GetTypeName(enumState.Datatype));
            sb.AppendLine("    {");

            for (int i = 0; i < enumValues.Count; ++i)
            {
                EnumValue enumValue = enumValues[i];

                sb.Append("        ");
                sb.Append(enumValue.Name);
                sb.Append(" = ");
                sb.Append(enumValue.Value);
                
                if (i < enumValues.Count - 1)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
    }
}
