using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Primary.Collections;
using Primary.Serialization.Arguments;

namespace Primary
{
    public static class AppArguments
    {
        private static FrozenDictionary<string, ArgumentFormat>? _argumentFormats = null;
        private static Dictionary<string, object> _arguments = new Dictionary<string, object>();

        internal static void Parse(ReadOnlySpan<string> args)
        {
            if (_argumentFormats == null)
                DeserializeArguments();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (_argumentFormats!.TryGetValue(arg, out ArgumentFormat? format))
                {
                    if (format.Type.Literal == ArgumentTypeLiteral.Null)
                    {
                        if (!_arguments.TryAdd(format.Name, s_defaultObject))
                        {
                            EngLog.Core.Warning("Duplicate argument '{n}'", arg);
                            continue;
                        }
                    }
                    else
                    {
                        if (i + 1 >= arg.Length)
                        {
                            EngLog.Core.Warning("Expected value for argument '{n}'", format.Name);
                            continue;
                        }

                        if (_arguments.ContainsKey(format.Name))
                        {
                            EngLog.Core.Warning("Duplicate argument '{n}'", arg);
                            continue;
                        }

                        if (format.Type.IsArray)
                        {
                            switch (format.Type.Literal)
                            {
                                case ArgumentTypeLiteral.Integer:
                                    {
                                        using RentedList<long> list = new RentedList<long>();
                                        while (i < args.Length)
                                        {
                                            ++i;

                                            string nextArg = args[++i];
                                            if (!string.IsNullOrEmpty(nextArg))
                                            {
                                                if (nextArg[0] == '"' && nextArg[^1] == '"')
                                                    nextArg = nextArg[1..^2];
                                                else if (nextArg[0] == '-')
                                                {
                                                    EngLog.Core.Warning("Expected value for argument '{n}'", format.Name);
                                                    continue;
                                                }
                                            }

                                            if (long.TryParse(nextArg, CultureInfo.InvariantCulture, out long result))
                                                list.Add(result);
                                            else
                                                EngLog.Core.Warning("Failed to parse value '{v}' for argument '{n}'", nextArg, format.Name);
                                        }

                                        _arguments.Add(format.Name, list.ToArray());
                                        break;
                                    }
                                case ArgumentTypeLiteral.Number:
                                    {
                                        using RentedList<double> list = new RentedList<double>();
                                        while (i < args.Length)
                                        {
                                            ++i;

                                            string nextArg = args[++i];
                                            if (!string.IsNullOrEmpty(nextArg))
                                            {
                                                if (nextArg[0] == '"' && nextArg[^1] == '"')
                                                    nextArg = nextArg[1..^2];
                                                else if (nextArg[0] == '-')
                                                {
                                                    EngLog.Core.Warning("Expected value for argument '{n}'", format.Name);
                                                    continue;
                                                }
                                            }

                                            if (double.TryParse(nextArg, CultureInfo.InvariantCulture, out double result))
                                                list.Add(result);
                                            else
                                                EngLog.Core.Warning("Failed to parse value '{v}' for argument '{n}'", nextArg, format.Name);
                                        }

                                        _arguments.Add(format.Name, list.ToArray());
                                        break;
                                    }
                                case ArgumentTypeLiteral.String:
                                    {
                                        using RentedList<string> list = new RentedList<string>();
                                        while (i < args.Length)
                                        {
                                            ++i;

                                            string nextArg = args[++i];
                                            if (!string.IsNullOrEmpty(nextArg))
                                            {
                                                if (nextArg[0] == '"' && nextArg[^1] == '"')
                                                    nextArg = nextArg[1..^2];
                                                else if (nextArg[0] == '-')
                                                {
                                                    EngLog.Core.Warning("Expected value for argument '{n}'", format.Name);
                                                    continue;
                                                }
                                            }

                                            list.Add(nextArg);
                                        }

                                        _arguments.Add(format.Name, list.ToArray());
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            string nextArg = args[++i];
                            if (!string.IsNullOrEmpty(nextArg))
                            {
                                if (nextArg[0] == '"' && nextArg[^1] == '"')
                                    nextArg = nextArg[1..^2];
                                else if (nextArg[0] == '-')
                                {
                                    EngLog.Core.Warning("Expected value for argument '{n}'", format.Name);
                                    continue;
                                }
                            }

                            switch (format.Type.Literal)
                            {
                                case ArgumentTypeLiteral.Integer:
                                    {
                                        if (long.TryParse(nextArg, CultureInfo.InvariantCulture, out long result))
                                            _arguments.Add(format.Name, result);
                                        else
                                            EngLog.Core.Warning("Failed to parse value '{v}' for argument '{n}'", nextArg, format.Name);
                                        break;
                                    }
                                case ArgumentTypeLiteral.Number:
                                    {
                                        if (double.TryParse(nextArg, CultureInfo.InvariantCulture, out double result))
                                            _arguments.Add(format.Name, result);
                                        else
                                            EngLog.Core.Warning("Failed to parse value '{v}' for argument '{n}'", nextArg, format.Name);
                                        break;
                                    }
                                case ArgumentTypeLiteral.String:
                                    {
                                        _arguments.Add(format.Name, nextArg);
                                        break;
                                    }
                            }
                        }
                    }
                }
                else
                {
                    EngLog.Core.Warning("Unknown argument '{arg}'", arg);
                    continue;
                }
            }
        }

        private static void DeserializeArguments()
        {
            Dictionary<string, ArgumentFormat> argumentFormats = new Dictionary<string, ArgumentFormat>();

            HashSet<string> includedFiles = new HashSet<string>();
            Queue<string> remainingFiles = new Queue<string>();

            remainingFiles.Enqueue(Path.GetFullPath("Arguments.json"));

            while (remainingFiles.TryDequeue(out string? result))
            {
                includedFiles.Add(result);

                ArgumentFile file;
                try
                {
                    using Stream stream = File.OpenRead(result);
                    file = JsonSerializer.Deserialize(stream, ArgumentJsonContext.Default.ArgumentFile)!;
                }
                catch (Exception ex)
                {
                    EngLog.Core.Error(ex, "Failed to load arguments file '{f}'", result);
                    continue;
                }

                foreach (ArgumentFormat format in file.Arguments)
                {
                    if (!argumentFormats.TryAdd($"--{format.Name}", format))
                    {
                        EngLog.Core.Warning("Argument format with the name '{n}' has already been added", format.Name);
                        continue;
                    }

                    if (format.Shorthand != null && !argumentFormats.TryAdd("-{format.Shorthand}", format))
                    {
                        EngLog.Core.Warning("Argument format with the shorthand '{n}' has already been added", format.Name);
                    }
                }

                foreach (string include in file.Includes)
                {
                    string fullPath = Path.GetFullPath(include);
                    if (!includedFiles.Contains(fullPath))
                        remainingFiles.Enqueue(fullPath);
                }
            }

            _argumentFormats = argumentFormats.ToFrozenDictionary();
        }

        public static bool HasArgument(string arg) => _arguments.ContainsKey(arg);

        public static bool TryGetValue<T>(string arg, [NotNullWhen(true)] out T? value)
        {
            if (_arguments.TryGetValue(arg, out object? r))
            {
                if (r is T t)
                {
                    value = t;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static T? GetValueOrDefault<T>(string arg, T? defaultValue)
        {
            if (_arguments.TryGetValue(arg, out object? value))
                return (T)value;
            return defaultValue;
        }

        public static bool TrySetValue<T>(string arg, T value) where T : notnull
        {
            if (_argumentFormats == null)
                DeserializeArguments();

            if (_argumentFormats!.TryGetValue($"--{arg}", out ArgumentFormat? format))
            {
                switch (format.Type.Literal)
                {
                    case ArgumentTypeLiteral.Null: return false;
                    case ArgumentTypeLiteral.Integer:
                        {
                            if (format.Type.IsArray && typeof(T).IsArray)
                            {
                                if (typeof(T).GetElementType() == typeof(long))
                                {
                                    _arguments[arg] = value;
                                    return true;
                                }
                            }
                            else
                            {
                                if (typeof(T) == typeof(long))
                                {
                                    _arguments[arg] = value;
                                    return true;
                                }    
                            }
                            break;
                        }
                    case ArgumentTypeLiteral.Number:
                        {
                            if (typeof(T).IsArray)
                            {
                                if (format.Type.IsArray && typeof(T).GetElementType() == typeof(double))
                                {
                                    _arguments[arg] = value;
                                    return true;
                                }
                            }
                            else
                            {
                                if (typeof(T) == typeof(double))
                                {
                                    _arguments[arg] = value;
                                    return true;
                                }
                            }
                            break;
                        }
                    case ArgumentTypeLiteral.String:
                        {
                            if (typeof(T).IsArray)
                            {
                                if (format.Type.IsArray && typeof(T).GetElementType() == typeof(string))
                                {
                                    _arguments[arg] = value;
                                    return true;
                                }
                            }
                            else
                            {
                                if (typeof(T) == typeof(string))
                                {
                                    _arguments[arg] = value;
                                    return true;
                                }
                            }
                            break;
                        }
                    case ArgumentTypeLiteral.Boolean:
                        {
                            if (typeof(T).IsArray)
                            {
                                if (format.Type.IsArray && typeof(T).GetElementType() == typeof(bool))
                                {
                                    _arguments[arg] = value;
                                    return true;
                                }
                            }
                            else
                            {
                                if (typeof(T) == typeof(bool))
                                {
                                    _arguments[arg] = value;
                                    return true;
                                }
                            }
                            break;
                        }
                }
            }

            return false;
        }

        public static bool TryAddValue(string arg)
        {
            if (_argumentFormats == null)
                DeserializeArguments();

            if (_argumentFormats!.TryGetValue(arg, out ArgumentFormat? format))
            {
                if (format.Type.Literal == ArgumentTypeLiteral.Null)
                {
                    _arguments[arg] = s_defaultObject;
                    return true;
                }
            }

            return false;
        }

        public static bool TryRemoveArgument(string arg) => _arguments.Remove(arg);

        private static readonly object s_defaultObject = new object();

        private readonly record struct ArgumentSpec(bool AllowMultiple, bool HasValue);
    }

    public sealed class ArgumentParseException : Exception
    {

    }
}
