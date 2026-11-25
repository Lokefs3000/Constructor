using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Serialization
{
    internal readonly record struct CompressionMetadata(bool IsCompressed, ushort DataLength);
}
