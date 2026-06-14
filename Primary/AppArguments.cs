using System.Collections.Frozen;
using System.Text.Json;
using Primary.Common;
using Primary.Serialization.Arguments;

namespace Primary
{
    public static class AppArguments
    {
        private static FrozenDictionary<string, ArgumentFormat>? _argumentFormats = null;
        private static FrozenDictionary<string, object> _arguments = FrozenDictionary<string, object>.Empty;
        
        internal static void Parse(ReadOnlySpan<string> args)
        {
            if (_argumentFormats == null)
                DeserializeArguments();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (_argumentFormats!.TryGetValue(arg, out ArgumentFormat? format))
                {
                    
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
                    if (!argumentFormats.TryAdd(format.Name, format))
                    {
                        EngLog.Core.Warning("Argument format with the name '{n}' has already been added", format.Name);
                        continue;
                    }

                    if (format.Shorthand != null && !argumentFormats.TryAdd(format.Shorthand, format))
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

        public static bool HasArgument(string arg) => false;

        private readonly record struct ArgumentSpec(bool AllowMultiple, bool HasValue);
    }

    public sealed class ArgumentParseException : Exception
    {

    }
}
