using System.Collections.Frozen;
using System.Text;
using CommunityToolkit.HighPerformance;
using PhysXBindGen.Data;

namespace PhysXBindGen.Emitters
{
    internal static class ClassEmitter
    {
        public static void Emit(ClassState classState, StringBuilder sb)
        {
            s_overloadedFunctions.Clear();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine();
            sb.AppendLine("namespace Interop.PhysX");
            sb.AppendLine("{");
            sb.Append("    public unsafe struct ");
            sb.Append(classState.Name);
            sb.AppendLine();
            sb.AppendLine("    {");

            ClassState? currentClass = classState;
            do
            {
                List<MethodState> methodStates = currentClass.Methods;
                for (int i = 0; i < methodStates.Count; ++i)
                {
                    MethodState methodState = methodStates[i];
                    if (!s_overloadedFunctions.Add(methodState.Overload == null ? methodState.Name : $"{methodState.Name}_{methodState.Overload}"))
                        continue;

                    List<MethodArgument> methodArguments = methodState.Arguments;

                    if (methodState.Comment != null)
                    {
                        sb.Append("        /// <inheritdoc cref=\"");

                        sb.Append("PhysXInterop.");
                        sb.Append(methodState.Owner!.Name);
                        sb.Append('_');
                        sb.Append(methodState.Name);

                        if (methodState.Overload != null)
                        {
                            sb.Append('_');
                            sb.Append(methodState.Overload);
                        }

                        sb.Append('(');

                        for (int j = 0; j < methodArguments.Count; j++)
                        {
                            MethodArgument argument = methodArguments[j];

                            string? addendum = null;
                            EmitterUtil.WriteTypeName(sb, argument.Type, ref addendum);
                            if (addendum != null)
                                sb.Append(addendum);

                            if (i < methodArguments.Count - 1)
                            {
                                sb.Append(", ");
                            }
                        }

                        sb.AppendLine(")\" />");
                    }

                    sb.Append("        public ");
                    {
                        string? addendum = null;
                        EmitterUtil.WriteTypeName(sb, methodState.ReturnType, ref addendum);
                        if (addendum != null)
                            sb.Append(addendum);
                    }

                    sb.Append(' ');
                    sb.Append(methodState.Name);
                    if (methodState.Name == "userData")
                    {
                        sb.Append('_');
                        sb.Append(methodState.Overload);
                    }
                    sb.Append('(');

                    for (int j = 0; j < methodArguments.Count; j++)
                    {
                        MethodArgument argument = methodArguments[j];
      
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

                        if (j < methodArguments.Count - 1)
                        {
                            sb.Append(", ");
                        }
                    }

                    sb.AppendLine(")");
                    sb.AppendLine("        {");

                    sb.Append("            ");
                    sb.Append(currentClass.Name);
                    sb.Append("* @this = (");
                    sb.Append(currentClass.Name);
                    sb.Append("*)Unsafe.AsPointer(ref Unsafe.AsRef<");
                    sb.Append(classState.Name);
                    sb.AppendLine(">(ref this));");

                    if (methodState.ReturnType is TypedefState typedefState && typedefState.IsBuiltIn && typedefState.Datatype == TypedefDatatype.Void)
                        sb.Append("            ");
                    else
                        sb.Append("            return ");

                    sb.Append("PhysXInterop.");
                    sb.Append(methodState.Owner!.Name);
                    sb.Append('_');
                    sb.Append(methodState.Name);

                    if (methodState.Overload != null)
                    {
                        sb.Append('_');
                        sb.Append(methodState.Overload);
                    }

                    if (methodArguments.Count > 0)
                    {
                        sb.Append("(@this, ");
                        for (int j = 0; j < methodArguments.Count; j++)
                        {
                            MethodArgument argument = methodArguments[j];

                            sb.Append(argument.Name);
                            if (j < methodArguments.Count - 1)
                            {
                                sb.Append(", ");
                            }
                        }
                    }
                    else
                    {
                        sb.Append("(@this");
                    }

                    sb.AppendLine(");");
                    sb.AppendLine("        }");

                    sb.AppendLine();
                }

                if (currentClass.InheritedClass != null)
                {
                    sb.Append("       // public static implicit operator ");
                    sb.Append(currentClass.InheritedClass.Name);
                    sb.Append("*(");
                    sb.Append(currentClass.Name);
                    sb.Append("* value) => (");
                    sb.Append(currentClass.InheritedClass.Name);
                    sb.AppendLine("*)value;");

                    sb.Append("       // public static explicit operator ");
                    sb.Append(currentClass.Name);
                    sb.Append("*(");
                    sb.Append(currentClass.InheritedClass.Name);
                    sb.Append("* value) => (");
                    sb.Append(currentClass.Name);
                    sb.AppendLine("*)value;");

                    sb.AppendLine();
                }
                else
                {
                    sb.Length -= 2;
                }

            } while ((currentClass = currentClass.InheritedClass) != null);

            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        private static readonly HashSet<string> s_overloadedFunctions = [];
    }
}
