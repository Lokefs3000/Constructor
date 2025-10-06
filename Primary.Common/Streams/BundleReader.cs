using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Common.Streams
{
    public sealed class BundleReader : IDisposable
    {
        private int? _hashCode;

        private Stream _stream;
        private bool _closeOnDispose;

        private long _baseOffset;

        private FrozenDictionary<string, BundleFileEntry> _entries;

        private bool _disposedValue;

        public BundleReader(Stream stream, bool closeOnDispose = false, string? sourcePath = null)
        {
            _hashCode = sourcePath?.GetHashCode();

            _stream = stream;
            _closeOnDispose = closeOnDispose;

            uint magic = stream.Read<uint>();
            uint version = stream.Read<uint>();

            if (magic == HeaderMagic || version == HeaderVersion)
            {
                BinaryReader br = new BinaryReader(stream, Encoding.UTF8, true);

                Dictionary<string, BundleFileEntry> entries = new Dictionary<string, BundleFileEntry>();

                uint fileCount = stream.Read<uint>();
                long currentOffset = 0;

                for (uint i = 0; i < fileCount; i++)
                {
                    string fileName = br.ReadString();
                    long size = (long)br.ReadUInt64();

                    entries.Add(fileName, new BundleFileEntry(currentOffset, size));
                    currentOffset += size;
                }

                _baseOffset = stream.Position;
                _entries = entries.ToFrozenDictionary();
            }
            else
            {
                _entries = FrozenDictionary<string, BundleFileEntry>.Empty;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_closeOnDispose)
                        _stream.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public string? ReadString(string path)
        {
            if (_entries.TryGetValue(path, out BundleFileEntry entry))
            {
                using PoolArray<byte> chars = ArrayPool<byte>.Shared.Rent((int)entry.Length);

                _stream.Seek(entry.Offset + _baseOffset, SeekOrigin.Begin);
                _stream.ReadExactly(chars.AsSpan(0, (int)entry.Length));

                return Encoding.UTF8.GetString(chars.AsSpan(0, (int)entry.Length));
            }

            return null;
        }

        public byte[]? ReadBytes(string path)
        {
            if (_entries.TryGetValue(path, out BundleFileEntry entry))
            {
                if (entry.Length == 0)
                    return Array.Empty<byte>();

                byte[] bytes = new byte[entry.Length];

                _stream.Seek(entry.Offset + _baseOffset, SeekOrigin.Begin);
                _stream.ReadExactly(bytes);

                return bytes;
            }

            return null;
        }

        public bool ReadBytes(string path, Span<byte> bytes)
        {
            if (_entries.TryGetValue(path, out BundleFileEntry entry))
            {
                if (bytes.IsEmpty)
                    return true;

                _stream.Seek(entry.Offset + _baseOffset, SeekOrigin.Begin);
                _stream.ReadExactly(bytes.Slice(0, Math.Min(bytes.Length, (int)entry.Length)));

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsFile(string path)
        {
            return _entries.ContainsKey(path);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetFileSize(string path)
        {
            if (_entries.TryGetValue(path, out BundleFileEntry entry))
                return entry.Length;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return _hashCode.GetValueOrDefault(base.GetHashCode());
        }

        private const uint HeaderMagic = 0x4c444e42;
        private const uint HeaderVersion = 0;

        private readonly record struct BundleFileEntry(long Offset, long Length);
    }
}
