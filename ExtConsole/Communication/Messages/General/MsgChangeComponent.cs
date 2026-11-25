using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Messages.General
{
    public struct MsgChangeComponent : IMessage
    {
        public ComponentId Component;

        public void Serialize(MessageWriter writer)
        {
            writer.Write((sbyte)Component);
        }

        public void Deserialize(MessageReader reader)
        {
            Component = (ComponentId)reader.ReadSByte();
        }

        public MessageId Id => MessageId.ChangeComponent;

        public enum ComponentId : sbyte
        {
            None = -1,

            Profiler = 0
        }
    }
}
