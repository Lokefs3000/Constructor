using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections;

namespace Primary.Logging
{
    public sealed class Logbook
    {
        private CircularBuffer<LogMessage> _log;
        private int _logMemoryUsage;

        private Lock _lock;

        internal Logbook()
        {
            _log = new CircularBuffer<LogMessage>(4096);
            _logMemoryUsage = 0;

            _lock = new Lock();
        }

        internal void AppendLogToBuffer(string logMessage, Exception? logException)
        {
            using (_lock.EnterScope())
            {
                _log.PushBack(new LogMessage(logMessage, logException));
                _logMemoryUsage += logMessage.Length + logMessage.Length;
            }
        }

        internal static void LogNewMessage(string logMessage, Exception? logException) => Engine.GlobalSingleton.Logbook.AppendLogToBuffer(logMessage, logException);
    }

    public readonly record struct LogMessage(string Message, Exception? Exception);
}
