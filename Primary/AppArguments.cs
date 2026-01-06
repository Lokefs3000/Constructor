using System;
using System.Collections.Generic;
using System.Text;

namespace Primary
{
    public static class AppArguments
    {
        private static HashSet<string> _arguments = new HashSet<string>();

        internal static void Parse(ReadOnlySpan<string> args)
        {
            //TODO: implement actual parsing
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                _arguments.Add(arg);
            }
        }

        public static bool HasArgument(string arg) => _arguments.Contains(arg);
    }

    public sealed class ArgumentParseException : Exception
    {

    }
}
