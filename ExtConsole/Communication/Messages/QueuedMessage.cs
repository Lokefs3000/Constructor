using ExtConsole.Communication.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication.Messages
{
    public readonly record struct QueuedMessage(MessageId Id, MessageReader Reader)
    {
        public T Deserialize<T>() where T : struct, IMessage
        {
            T t = default;
            t.Deserialize(Reader);

            return t;
        }
    }
}
