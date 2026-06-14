using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;

namespace PrimaryEditor.Assets
{
    public sealed class PhysicalFileRegistry
    {
        private SHA256 _hashAlgorithm;
        private ConcurrentDictionary<AssetId, PhysicalFileInfo> _physicalFiles;

        internal PhysicalFileRegistry()
        {
            _hashAlgorithm = SHA256.Create();
            _physicalFiles = new ConcurrentDictionary<AssetId, PhysicalFileInfo>();
        }

        internal bool TryRegisterFile(AssetId id, DateTime lastWriteTime, long fileSize, FileChecksum checksum)
        {
            if (!_physicalFiles.TryAdd(id, new PhysicalFileInfo(lastWriteTime, fileSize, checksum)))
            {
                EdLog.Assets.Warning("Failed to register new physical file with the id '{id}' because it's already in the dictionary", id);
                return false;
            }

            return true;
        }

        public bool TryGetFileInfo(AssetId id, [NotNullWhen(true)] out PhysicalFileInfo value)
        {
            return _physicalFiles.TryGetValue(id, out value);
        }

        public FileChecksum CreateChecksumFrom(Stream stream)
        {
            return new FileChecksum(_hashAlgorithm.ComputeHash(stream));
        }
    }

    public readonly record struct PhysicalFileInfo(DateTime LastModifiedTime, long FileSize, FileChecksum Checksum);

    public readonly record struct FileChecksum : IEquatable<FileChecksum>
    {
        public readonly long Part0;
        public readonly long Part1;
        public readonly long Part2;
        public readonly long Part3;

        public FileChecksum(ReadOnlySpan<byte> hash)
        {
            this = Unsafe.ReadUnaligned<FileChecksum>(ref hash.DangerousGetReference());
        }
    }
}
