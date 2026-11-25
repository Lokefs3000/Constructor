using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Exceptions;

namespace PrimaryEditor.Assets.Exceptions
{
    public sealed class AssetPipelineException : LoggerException
    {
        public AssetPipelineException()
        {
        }

        public AssetPipelineException(string? message) : base(message)
        {
        }

        public AssetPipelineException(string? message, params object?[]? args) : base(message, args)
        {
        }
    }
}
