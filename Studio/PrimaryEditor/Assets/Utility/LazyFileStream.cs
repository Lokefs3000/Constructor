using System;
using System.Collections.Generic;
using System.Text;
using Primary.Common;
using PrimaryEditor.Assets.Exceptions;

namespace PrimaryEditor.Assets.Utility
{
    public sealed class LazyFileStream : Stream
    {
        private readonly string _filePath;
        private readonly FileMode _mode;
        private readonly FileAccess _access;
        private readonly FileShare _share;
        private readonly int _maxTries;
        private readonly int _timeoutMs;

        private FileStream? _stream;

        public LazyFileStream(string fullPath, FileMode mode, FileAccess access, FileShare share, int maxTries = 10, int timeoutMs = 250)
        {
            _filePath = fullPath;
            _mode = mode;
            _access = access;
            _share = share;
            _maxTries = maxTries;
            _timeoutMs = timeoutMs;

            _stream = null;
        }

        protected override void Dispose(bool disposing)
        {
            _stream?.Flush(true);

            _stream?.Dispose();
            _stream = null;
        }

        private FileStream TryOpenStream()
        {
            FileStream? stream = FileUtility.TryWaitOpenNoThrow(_filePath, _mode, _access, _share, _maxTries, _timeoutMs);

            if (stream == null)
            {
                EdLog.Assets.Error("Failed to open stream for path '{p}'", _filePath);
                throw new AssetImportException();
            }

            return stream;
        }

        #region Stream
        public override void Flush()
        {
            (_stream ??= TryOpenStream()).Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return (_stream ??= TryOpenStream()).Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return (_stream ??= TryOpenStream()).Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            (_stream ??= TryOpenStream()).SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            (_stream ??= TryOpenStream()).Write(buffer, offset, count);
        }

        public override bool CanRead => (_stream ??= TryOpenStream()).CanRead;
        public override bool CanSeek => (_stream ??= TryOpenStream()).CanSeek;
        public override bool CanWrite => (_stream ??= TryOpenStream()).CanWrite;

        public override long Length => (_stream ??= TryOpenStream()).Length;
        public override long Position { get => (_stream ??= TryOpenStream()).Position; set => (_stream ??= TryOpenStream()).Position = value; }
        #endregion
    }
}
