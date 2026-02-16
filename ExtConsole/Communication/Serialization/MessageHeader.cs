using ExtConsole.Communication.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Serialization
{
    public struct MessageHeader
    {
        public uint Header;
        public ushort MessageSize;
        public MessageId Id;

        public const uint ConHeader = 0x47534d4e;
    }
}
