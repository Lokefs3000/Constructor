using CommandLine;
using CommunityToolkit.HighPerformance;
using Editor.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Runners
{
    internal class BundleRunner
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

            BundleProcessor processor = new BundleProcessor();
            processor.Execute(new BundleProcessorArgs
            {
                AbsoluteFilepaths = new Memory<string>(value.Inputs.ToArray()),
                AbsoluteOutputPath = Path.GetFullPath(value.Output),
            });
        }

        private class RunnerArguments
        {
            [Option('i', "input")]
            public IList<string> Inputs { get; set; } = new List<string>();
            [Option('o', "output")]
            public string Output { get; set; } = string.Empty;
        }
    }
}
