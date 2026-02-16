using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Net
{
    internal interface IDataClient
    {
        public bool IsConnected { get; }

        public void SendRaw(ReadOnlySpan<byte> bytes);
    }
}
