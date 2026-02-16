using ExtConsole.Communication;
using ExtConsole.Communication.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.ExtConsole
{
    internal interface ISubComponent
    {
        public void UpdatePackets(ExtConsoleClient client);
        public void RecievePacket(QueuedMessage queued);
    }
}
