using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using PhysXBindGen.Data;

namespace PhysXBindGen.Emitters
{
    internal static class MethodEmitter
    {
        internal static void Emit(List<MethodState> methodStates, StringBuilder sb)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.InteropServices;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine();
            sb.AppendLine("[assembly: DisableRuntimeMarshalling]");
            sb.AppendLine();
            sb.AppendLine("namespace Interop.PhysX");
            sb.AppendLine("{");
            sb.AppendLine("    public static unsafe partial class PhysXInterop");
            sb.AppendLine("    {");

            foreach (MethodState methodState in methodStates)
            {
                if (methodState.Comment != null)
                {
                    sb.Append("        /// <summary>");
                    foreach (var token in methodState.Comment.Tokenize('\n'))
                    {
                        sb.Append(token.Trim());
                        sb.Append("<br/>");
                    }
                    sb.AppendLine("</summary>");
                }

                sb.Append("        [LibraryImport(\"physx_interop_x64\", EntryPoint = \"C");
                {
                    if (methodState.Owner != null)
                    {
                        sb.Append(methodState.Owner.Name);
                        sb.Append('_');
                    }

                    sb.Append(methodState.Name.StartsWith('C') ? methodState.Name.AsSpan(1) : methodState.Name);

                    if (methodState.Overload != null)
                    {
                        sb.Append('_');
                        sb.Append(methodState.Overload);
                    }
                }
                sb.AppendLine("\")]");

                {
                    if (methodState.ReturnType is TypedefState typedef && typedef.Datatype == TypedefDatatype.Bool)
                    {
                        sb.AppendLine("        [return: MarshalAs(UnmanagedType.I1)]");
                    }
                }

                sb.Append("        public static partial ");
                {
                    string? addendum = null;
                    EmitterUtil.WriteTypeName(sb, methodState.ReturnType, ref addendum);
                    if (addendum != null)
                        sb.Append(addendum);
                }

                sb.Append(' ');
                {
                    if (methodState.Owner != null)
                    {
                        sb.Append(methodState.Owner.Name);
                        sb.Append('_');
                    }

                    sb.Append(methodState.Name.StartsWith('C') ? methodState.Name.AsSpan(1) : methodState.Name);

                    if (methodState.Overload != null)
                    {
                        sb.Append('_');
                        sb.Append(methodState.Overload);
                    }
                }

                sb.Append('(');
                if (methodState.Owner != null)
                {
                    sb.Append(methodState.Owner.Name);
                    if (methodState.Arguments.Count > 0)
                        sb.Append("* @this, ");
                    else
                        sb.Append("* @this");
                }

                List<MethodArgument> methodArguments = methodState.Arguments;
                for (int i = 0; i < methodArguments.Count; i++)
                {
                    MethodArgument argument = methodArguments[i];

                    {
                        if (argument.Type is TypedefState typedef && typedef.Datatype == TypedefDatatype.Bool)
                        {
                            sb.Append("[MarshalAs(UnmanagedType.I1)] ");
                        }
                    }

                    string? addendum = null;
                    EmitterUtil.WriteTypeName(sb, argument.Type, ref addendum);
                    if (addendum != null)
                        sb.Append(addendum);
                    sb.Append(' ');
                    sb.Append(argument.Name);

                    if (argument.DefaultValue.HasValue)
                    {
                        double defaultVal = argument.DefaultValue.Value;
                        if (argument.Type is PointerDatatype)
                        {
                            sb.Append(" = null");
                        }
                        else if (argument.Type is TypedefState typedef)
                        {
                            if (typedef.Datatype == TypedefDatatype.Bool)
                            {
                                sb.Append(defaultVal == 1.0 ? " = true" : " = false");
                            }
                            else if (typedef.Datatype == TypedefDatatype.Single)
                            {
                                sb.Append(" = ");
                                sb.Append((float)defaultVal);
                                sb.Append('f');
                            }
                            else if (typedef.Datatype == TypedefDatatype.Double)
                            {
                                sb.Append(" = ");
                                sb.Append(defaultVal);
                            }
                            else
                            {
                                sb.Append(" = ");
                                sb.Append((long)defaultVal);
                            }
                        }
                        else if (argument.Type is EnumState enumState)
                        {
                            long defaultValLong = (long)defaultVal;

                            sb.Append(" = ");
                            if (enumState.TryGetName(defaultValLong, out string? name))
                            {
                                sb.Append(enumState.Name.AsSpan(1));
                                sb.Append('.');
                                sb.Append(name);
                            }
                            else
                            {
                                sb.Append('(');
                                sb.Append(enumState.Name.AsSpan(1));
                                sb.Append(')');
                                sb.Append(defaultValLong);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }

                    if (i < methodArguments.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }

                sb.AppendLine(");");
                sb.AppendLine();
            }

            sb.Length -= 2;

            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
    }
}
