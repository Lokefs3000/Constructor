using CommandLine;
using Editor.Processors;

namespace Editor.Runners
{
    internal class ModelRunner
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

            ModelProcessor processor = new ModelProcessor();
            processor.Execute(new ModelProcessorArgs
            {
                AbsoluteFilepath = Path.GetFullPath(value.Input),
                AbsoluteOutputPath = Path.GetFullPath(value.Output)
            });
        }

        private class RunnerArguments
        {
            [Option('i', "input")]
            public string Input { get; set; } = string.Empty;
            [Option('o', "output")]
            public string Output { get; set; } = string.Empty;
        }
    }
}
