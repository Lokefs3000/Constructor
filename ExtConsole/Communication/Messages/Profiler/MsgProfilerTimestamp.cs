using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Messages.Profiler
{
    public struct MsgProfilerTimestamp : IMessage
    {
        public string Name;
        public long PrId;
        public ushort Depth;

        public long Start;
        public long End;

        public ushort Allocated;

        public void Serialize(MessageWriter writer)
        {
            writer.Write(Name);
            writer.Write(PrId);
            writer.Write(Depth);

            writer.Write(Start);
            writer.Write(End);

            writer.Write(Allocated);
        }

        public void Deserialize(MessageReader reader)
        {
            Name = reader.ReadString();
            PrId = reader.ReadInt64();
            Depth = reader.ReadUInt16();

            Start = reader.ReadInt64();
            End = reader.ReadInt64();

            Allocated = reader.ReadUInt16();
        }

        public MessageId Id => MessageId.ProfilerTimestamp;
    }
}
