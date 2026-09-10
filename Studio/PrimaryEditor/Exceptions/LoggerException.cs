using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Exceptions
{
    public abstract class LoggerException : Exception
    {
        private readonly object?[]? _args;

        protected LoggerException()
        {
        }

        protected LoggerException(string? message) : base(message)
        {
            _args = null;
        }

        protected LoggerException(string? message, params object?[]? args) : base(message)
        {
            _args = args;
        }

        public object?[]? Arguments => _args;
    }
}
