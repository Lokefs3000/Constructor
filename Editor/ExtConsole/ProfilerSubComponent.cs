using ExtConsole.Communication;
using ExtConsole.Communication.Messages;
using ExtConsole.Communication.Messages.Profiler;
using Primary.Common;
using Primary.Profiling;
using Primary.Timing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.ExtConsole
{
    internal sealed class ProfilerSubComponent : ISubComponent
    {
        public void UpdatePackets(ExtConsoleClient client)
        {
            ProfilingManager profiler = Editor.GlobalSingleton.ProfilingManager;
            if (profiler.Timestamps.Count > 0)
            {
                ushort count = 0;
                foreach (var kvp in profiler.Timestamps)
                    count += (ushort)kvp.Value.Timestamps.Count;

                client.SendMessage(new MsgNewProfilerData
                {
                    TimestampCount = count,
                    Frametime = Time.DeltaTimeDouble
                });

                foreach (var kvp in profiler.Timestamps)
                {
                    foreach (ProfilingTimestamp timestamp in kvp.Value.Timestamps)
                    {
                        client.SendMessage(new MsgProfilerTimestamp
                        {
                            Name = timestamp.Name,
                            PrId = (long)timestamp.Id.Code << 32 | (long)timestamp.Id.Hash,
                            Depth = (ushort)timestamp.Depth,

                            Start = timestamp.StartTimestamp,
                            End = timestamp.EndTimestamp,

                            Allocated = (ushort)timestamp.Allocated
                        });
                    }
                }
            }
        }

        public void RecievePacket(QueuedMessage queued)
        {
            switch (queued.Id)
            {
                case MessageId.SetProfilerFeatures:
                    {
                        Editor editor = Editor.GlobalSingleton;
                        ProfilingManager profiler = editor.ProfilingManager;

                        MsgSetProfilerFeatures msg = queued.Deserialize<MsgSetProfilerFeatures>();

                        ProfilingOptions options = ProfilingOptions.None;
                        if (Flags.HasFlag(msg.Features, MsgSetProfilerFeatures.ProfilerFeatures.Allocation))
                            options |= ProfilingOptions.CollectAllocation;

                        ProfilingManager.Options = options;
                        break;
                    }
            }
        }
    }
}
