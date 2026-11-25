using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Assets
{
    public sealed class AssetNotReadyException : Exception
    {
        public AssetNotReadyException()
        {
        }

        public AssetNotReadyException(string? message) : base(message)
        {
        }
    }
}
