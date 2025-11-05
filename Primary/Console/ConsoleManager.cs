using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Tree;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Primary.Console
{
    public sealed class ConsoleManager
    {
        private readonly Dictionary<string, ConsoleVariable> _variables;

        internal ConsoleManager()
        {
            _variables = new Dictionary<string, ConsoleVariable>();

            ScanClassForCVars(typeof(RenderingCVars));
            ScanClassForCVars(typeof(TreeCVars));

            EngLog.Console.Information("Found #{c} cvars in classes..", _variables.Count);
        }

        private void EvaluateInputString(string input)
        {
            EngLog.Console.Information("{in}", input.ToString());

            ReadOnlySpanTokenizer<char> tokenizer = input.Tokenize(' ');
            if (!tokenizer.MoveNext())
            {
                EngLog.Console.Error("Malformed input console input string");
                return;
            }

            string cvarName = tokenizer.Current.ToString();
            if (!_variables.TryGetValue(cvarName, out ConsoleVariable variable))
            {
                EngLog.Console.Error("Undefined cvar: {cv}", cvarName);
                return;
            }

            if (tokenizer.MoveNext())
            {
                IGenericCVar generic = (IGenericCVar)variable.Field.GetValue(null)!;
                Type fieldType = generic.VariableType;

                ReadOnlySpan<char> value = tokenizer.Current.Trim();
                if (value.IsEmpty)
                    goto PrintValue;

                bool ret = false;

                switch (Type.GetTypeCode(fieldType))
                {
                    case TypeCode.Boolean: ret = Evaluate_Boolean(cvarName, value, generic, out generic); break;
                    case TypeCode.SByte: ret = Evaluate_Number<sbyte>(cvarName, value, generic, out generic); break;
                    case TypeCode.Byte: ret = Evaluate_Number<byte>(cvarName, value, generic, out generic); break;
                    case TypeCode.Int16: ret = Evaluate_Number<short>(cvarName, value, generic, out generic); break;
                    case TypeCode.UInt16: ret = Evaluate_Number<ushort>(cvarName, value, generic, out generic); break;
                    case TypeCode.Int32: ret = Evaluate_Number<int>(cvarName, value, generic, out generic); break;
                    case TypeCode.UInt32: ret = Evaluate_Number<uint>(cvarName, value, generic, out generic); break;
                    case TypeCode.Int64: ret = Evaluate_Number<long>(cvarName, value, generic, out generic); break;
                    case TypeCode.UInt64: ret = Evaluate_Number<ulong>(cvarName, value, generic, out generic); break;
                    case TypeCode.Single: ret = Evaluate_Number<float>(cvarName, value, generic, out generic); break;
                    case TypeCode.Double: ret = Evaluate_Number<double>(cvarName, value, generic, out generic); break;
                    case TypeCode.String: ret = Evaluate_String(cvarName, value, input, generic, out generic); break;
                    default:
                        {
                            EngLog.Console.Error("[{cv}]: Unsupported type code: {t}", cvarName, Type.GetTypeCode(fieldType));
                            return;
                        }
                }

                if (!ret)
                {
                    EngLog.Console.Error("[{cv}]: Argument malformed or empty", cvarName);
                }
                else
                {
                    foreach (ICVarModifier modifier in variable.Modifiers)
                    {
                        if (modifier is CVarRangeAttribute range)
                        {
                            IComparable? obj = generic.GetValue() as IComparable;
                            if (obj != null)
                            {
                                if (obj.CompareTo(range.Min) < 0)
                                    generic.SetValue(Convert.ChangeType(range.Min, fieldType) ?? obj);
                                else if (obj.CompareTo(range.Max) > 0)
                                    generic.SetValue(Convert.ChangeType(range.Max, fieldType) ?? obj);
                            }
                        }
                    }

                    variable.Field.SetValue(null, generic);
                    EngLog.Console.Information("[{cv}]: Set to new value: {nv}", cvarName, generic.GetValue());
                }

                return;
            }

        PrintValue:
            EngLog.Console.Information("[{cv}]: {val}", cvarName, variable.Field.GetValue(null));

            static bool Evaluate_Boolean(string cvarName, ReadOnlySpan<char> value, IGenericCVar cvar, out IGenericCVar updatedCVar)
            {
                if (value.IsEmpty)
                {
                    updatedCVar = cvar;
                    return false;
                }

                CVar<bool> typed = (CVar<bool>)cvar;
                if (value.Length == 1)
                {
                    if (value[0] == '0')
                    {
                        typed.Value = true;
                        updatedCVar = typed;
                        return true;
                    }
                    else if (value[0] == '1')
                    {
                        typed.Value = false;
                        updatedCVar = typed;
                        return true;
                    }
                }
                else if (bool.TryParse(value, out bool result))
                {
                    typed.Value = result;
                    updatedCVar = typed;
                    return true;
                }

                updatedCVar = cvar;
                return false;
            }

            static bool Evaluate_Number<T>(string cvarName, ReadOnlySpan<char> value, IGenericCVar cvar, out IGenericCVar updatedCVar) where T : struct, INumberBase<T>
            {
                CVar<T> typed = (CVar<T>)cvar;

                object? result = Convert.ChangeType(value.ToString(), typeof(T));
                if (result != null)
                {
                    typed.Value = (T)result;
                    updatedCVar = typed;
                    return true;
                }

                //TODO: implement expression parsing
                updatedCVar = cvar;
                return false;
            }

            static bool Evaluate_String(string cvarName, ReadOnlySpan<char> value, ReadOnlySpan<char> line, IGenericCVar cvar, out IGenericCVar updatedCVar)
            {
                if (value.IsEmpty)
                {
                    updatedCVar = cvar;
                    return false;
                }

                CVar<string> typed = (CVar<string>)cvar;

                if (value[0] == '"')
                {
                    if (!line.EndsWith('"'))
                    {
                        updatedCVar = cvar;
                        return false;
                    }

                    typed.Value = line.Slice(cvarName.Length + 2, line.Length - 3).ToString();
                    updatedCVar = typed;
                    return true;
                }

                typed.Value = value.ToString();
                updatedCVar = typed;
                return true;
            }
        }

        internal void ScanClassForCVars([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type)
        {
            CommandClassNamespaceAttribute? namespaceAttribute = type.GetCustomAttribute<CommandClassNamespaceAttribute>();
            Checking.Assert(namespaceAttribute != null, "CVar class must have a namespace attribute attached");

            List<ICVarModifier> genericModifiers = new List<ICVarModifier>();

            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                string name;

                CommandAliasAttribute? aliasAttribute = field.GetCustomAttribute<CommandAliasAttribute>();
                if (aliasAttribute != null)
                    name = $"{namespaceAttribute.Namespace}_{aliasAttribute.Alias}";
                else
                    name = $"{namespaceAttribute.Namespace}_{field.Name.ToLowerInvariant()}";

                genericModifiers.Clear();
                foreach (Attribute data in field.GetCustomAttributes())
                {
                    if (data is ICVarModifier modifier)
                    {
                        genericModifiers.Add(modifier);
                    }
                }

                if (!_variables.TryAdd(name, new ConsoleVariable(field, genericModifiers.ToArray())))
                {
                    EngLog.Console.Error("Failed to add cvar as a different one already exists with the name: {n} (field: {f}, class: {c})", name, field.Name, type.Name);
                }
            }
        }

        public static void Process(string input) => Engine.GlobalSingleton.ConsoleManager.EvaluateInputString(input);

        public static object? GetGenericValue(string variableName)
        {
            ConsoleManager @this = Engine.GlobalSingleton.ConsoleManager;
            if (!@this._variables.TryGetValue(variableName, out ConsoleVariable fi))
            {
                return null;
            }

            IGenericCVar cvar = (IGenericCVar)fi.Field.GetValue(null)!;
            return cvar.GetValue();
        }

        public static IEnumerable<string> Variables => Engine.GlobalSingleton.ConsoleManager._variables.Keys;

        private readonly record struct ConsoleVariable(FieldInfo Field, ICVarModifier[] Modifiers);
    }
}
