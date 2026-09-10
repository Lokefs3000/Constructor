using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Processors
{
    public sealed class AssetProcessorException : Exception
    {
        public AssetProcessorException()
        {
        }

        public AssetProcessorException(string? message) : base(message)
        {
        }
    }
}
