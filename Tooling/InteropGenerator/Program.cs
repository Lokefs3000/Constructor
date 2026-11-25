using System.Numerics;
using ClangSharp.Interop;
using CommandLine;
using HexaGen.CppAst.Model.Metadata;
using HexaGen.CppAst.Parsing;
using Serilog;

namespace InteropGenerator
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Parser.Default.ParseArguments<CommandArgs>(args)
                .WithParsed(ProcessInputs)
                .WithNotParsed(ReportParseError);
        }

        private static void ProcessInputs(CommandArgs args)
        {
            try
            {
                ArraySegment<string> expandFiles = ExpandFilesFrom(args.Inputs);

                CppCompilation compilation = CppParser.ParseFiles([.. expandFiles]);

                ClassGenerator classGenerator = new ClassGenerator();
                classGenerator.Generate(args, compilation.Classes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unhandled exception in generation");
            }
        }

        private static void ReportParseError(IEnumerable<Error> errors)
        {
            foreach (Error error in errors)
            {
                Log.Error("{x}", error.Tag);
            }
        }

        private static string[] ExpandFilesFrom(IEnumerable<string> paths)
        {
            HashSet<string> includedFiles = new HashSet<string>();

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    foreach (string file in Directory.EnumerateFiles(path, "*.*"))
                    {
                        if (file.EndsWith(".h", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".hpp", StringComparison.OrdinalIgnoreCase))
                            includedFiles.Add(Path.GetFullPath(file));
                    }
                }
                else
                {
                    includedFiles.Add(Path.GetFullPath(path));
                }
            }

            return [.. includedFiles];
        }
    }
}
