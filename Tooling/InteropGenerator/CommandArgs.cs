using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace InteropGenerator
{
    public sealed class CommandArgs
    {
        [Option('i', "input", Required = true)]
        public IEnumerable<string> Inputs { get; set; }

        [Option('o', "output", Required = true)]
        public string Output { get; set; }
    }
}
