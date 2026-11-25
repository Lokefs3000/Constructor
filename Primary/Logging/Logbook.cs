using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Collections;

namespace Primary.Logging
{
    public sealed class Logbook
    {
        private readonly int _maxMemoryUsage;

        private DynamicCircularBuffer<LogMessage> _log;
        private int _logMemoryUsage;

        private Lock _lock;

        public Logbook()
        {
            s_isLogbookEnabled = AppArguments.GetValueOrDefault("no-logbook", true);

            _maxMemoryUsage = AppArguments.GetValueOrDefault("logbook-max-size", DefaultLogbookCapacity);

            _log = new DynamicCircularBuffer<LogMessage>(32);
            _logMemoryUsage = 0;

            _lock = new Lock();
        }

        private void AppendLogToBuffer(string logMessage, Exception? logException)
        {
            int logSize = Math.Min(logMessage.Length * Unsafe.SizeOf<char>(), 16);

            using (_lock.EnterScope())
            {
                if (_logMemoryUsage + logSize > _maxMemoryUsage)
                {
                    if (_log.IsEmpty)
                        return;

                    do
                    {
                        LogMessage message = _log.PopFront();
                        _logMemoryUsage -= message.ByteSize;
                    } while (_logMemoryUsage + logSize > _maxMemoryUsage);
                }

                _log.PushBack(new LogMessage(logMessage, logException, logSize));
                _logMemoryUsage += logSize;
            }
        }

        internal static void LogNewMessage(string logMessage, Exception? logException)
        {
            if (s_isLogbookEnabled)
                Engine.GlobalSingleton.Logbook.AppendLogToBuffer(logMessage, logException);
        }

        private static bool s_isLogbookEnabled;

        public const int DefaultLogbookCapacity = 32768;
    }

    public readonly record struct LogMessage(string Message, Exception? Exception, int ByteSize);
}
