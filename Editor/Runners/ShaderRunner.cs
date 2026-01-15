using CommandLine;
using Editor.Processors;

namespace Editor.Runners
{
    internal class ShaderRunner
    {
        public void Execute(Span<string> args)
        {
            ParserResult<RunnerArguments> result = Parser.Default.ParseArguments<RunnerArguments>(args.ToArray());
            if (result.Errors.Any())
            {
                bool shouldReturnBad = false;

                foreach (Error error in result.Errors)
                {
                    Console.WriteLine(error.ToString());

                    if (error.StopsProcessing)
                    {
                        shouldReturnBad = true;
                    }
                }

                if (shouldReturnBad)
                {
                    return;
                }
            }

            RunnerArguments value = result.Value;

            ShaderProcessor processor = new ShaderProcessor();
            //processor.Execute(new ShaderProcessorArgs
            //{
            //    AbsoluteFilepath = Path.GetFullPath(value.Input),
            //    AbsoluteOutputPath = Path.GetFullPath(value.Output),
            //
            //    ContentSearchDirs = [Path.GetFullPath(value.ContentSearchDir), EditorFilepaths.EnginePath, EditorFilepaths.EditorPath],
            //
            //    Target = value.API
            //});
        }

        private class RunnerArguments
        {
            [Option('i', "input")]
            public string Input { get; set; } = string.Empty;
            [Option('o', "output")]
            public string Output { get; set; } = string.Empty;

            [Option('c', "content")]
            public string ContentSearchDir { get; set; } = string.Empty;

            //[Option('t', "target")]
            //public RHI.GraphicsAPI API { get; set; } = RHI.GraphicsAPI.None;
        }
    }
}
