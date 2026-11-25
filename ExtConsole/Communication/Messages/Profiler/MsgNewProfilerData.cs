using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Messages.Profiler
{
    public struct MsgNewProfilerData : IMessage
    {
        public ushort TimestampCount;
        public double Frametime;

        public void Serialize(MessageWriter writer)
        {
            writer.Write(TimestampCount);
            writer.Write(Frametime);
        }

        public void Deserialize(MessageReader reader)
        {
            TimestampCount = reader.ReadUInt16();
            Frametime = reader.ReadDouble();
        }

        public MessageId Id => MessageId.NewProfilerData;
    }
}
