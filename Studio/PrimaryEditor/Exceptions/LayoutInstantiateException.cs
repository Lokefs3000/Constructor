using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Exceptions
{
    public sealed class LayoutInstantiateException : Exception
    {
        public LayoutInstantiateException()
        {
        }

        public LayoutInstantiateException(string? message) : base(message)
        {
        }

        public LayoutInstantiateException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
