using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Assets.Exceptions
{
    public sealed class AssetImportException : Exception
    {
        public AssetImportException()
        {
        }

        public AssetImportException(string? message) : base(message)
        {
        }

        public AssetImportException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
