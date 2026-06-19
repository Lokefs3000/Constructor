using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Common;
using PrimaryEditor.Assets.Serialization;
using PrimaryEditor.Project;

namespace PrimaryEditor.Assets
{
    public sealed class PhysicalFileRegistry
    {
        private ConcurrentDictionary<AssetId, PhysicalFileInfo> _physicalFiles;

        internal PhysicalFileRegistry()
        {
            _physicalFiles = new ConcurrentDictionary<AssetId, PhysicalFileInfo>();
        }

        internal void LoadRegistryFromDisk()
        {
            if (File.Exists(s_registryFile))
            {
                PhysicalFileJson json;
                try
                {
                    json = JsonSerializer.Deserialize(File.ReadAllText(s_registryFile), PhysicalFileJsonContext.Default.PhysicalFileJson)!;
                }
                catch (Exception ex)
                {
                    EdLog.Assets.Error(ex, "Failed to read physical file registry from disk!");
                    return;
                }

                if (json.Version != PhysicalFileJson.FileVersion)
                {
                    EdLog.Assets.Error("Incorrect physical file registry version '{v}'", json.Version);
                    return;
                }

                foreach (var (key, value) in json.Files)
                {
                    if (!_physicalFiles.TryAdd(key, value))
                    {
                        EdLog.Assets.Warning("Duplicate physical file info id '{k}'", key);
                    }
                }
            }
        }

        internal void SaveRegistryToDisk()
        {
            PhysicalFileJson data = new PhysicalFileJson();

            data.Files = [.. _physicalFiles];

            try
            {
                File.WriteAllText(s_registryFile, JsonSerializer.Serialize(data, PhysicalFileJsonContext.Default.PhysicalFileJson));
            }
            catch (Exception ex)
            {
                EdLog.Assets.Error(ex, "Failed to read file remappings from disk!");
                throw;
            }
        }

        internal void TryRegisterFileFromPath(AssetId id, string fullPath)
        {
            _physicalFiles.AddOrUpdate(id, New, (id, fileInfo, arg) => New(id, arg), fullPath);

            PhysicalFileInfo New(AssetId id, string fullPath)
            {
                using Stream stream = FileUtility.TryWaitOpen(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                return new PhysicalFileInfo(
                    File.GetLastWriteTime(fullPath),
                    stream.Length,
                    CreateChecksumFrom(stream));
            }
        }

        internal void TryUpdateFileFromPath(AssetId id, string fullPath)
        {
            _physicalFiles.AddOrUpdate(id, New, (id, fileInfo, arg) => New(id, arg), fullPath);

            PhysicalFileInfo New(AssetId id, string fullPath)
            {
                using Stream stream = FileUtility.TryWaitOpen(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                return new PhysicalFileInfo(
                    File.GetLastWriteTime(fullPath),
                    stream.Length,
                    CreateChecksumFrom(stream));
            }
        }

        internal bool HasRegisteredFile(AssetId id)
        {
            return _physicalFiles.ContainsKey(id);
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
            return new FileChecksum((s_hashAlgorithm ??= SHA256.Create()).ComputeHash(stream));
        }

        private static string s_registryFile => Path.Combine(ProjectData.Instance.Paths.LibrarySavedFolder, "PhysicalFiles.json");

        [ThreadStatic]
        private static SHA256? s_hashAlgorithm;
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
