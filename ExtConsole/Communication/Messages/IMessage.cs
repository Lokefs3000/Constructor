using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Messages
{
    public interface IMessage
    {
        public MessageId Id { get; }

        public void Serialize(MessageWriter writer);
        public void Deserialize(MessageReader reader);
    }

    public enum MessageId : byte
    {
        Unknown = 0,

        ChangeComponent,

        NewProfilerData,
        ProfilerTimestamp,
        SetProfilerFeatures
    }
}
