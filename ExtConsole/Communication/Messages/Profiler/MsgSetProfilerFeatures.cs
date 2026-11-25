using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Messages.Profiler
{
    public struct MsgSetProfilerFeatures : IMessage
    {
        public ProfilerFeatures Features;

        public void Serialize(MessageWriter writer)
        {
            writer.Write((byte)Features);
        }

        public void Deserialize(MessageReader reader)
        {
            Features = (ProfilerFeatures)reader.ReadByte();
        }

        public MessageId Id => MessageId.SetProfilerFeatures;

        public enum ProfilerFeatures : byte
        {
            Allocation = 1 << 0
        }
    }
}
