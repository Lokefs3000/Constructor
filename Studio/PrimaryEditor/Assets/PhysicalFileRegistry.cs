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
        private ConcurrentDictionary<FileId, PhysicalFileInfo> _physicalFiles;

        internal PhysicalFileRegistry()
        {
            _physicalFiles = new ConcurrentDictionary<FileId, PhysicalFileInfo>();
        }

        internal void LoadRegistryFromDisk()
        {
            if (File.Exists(s_registryFile))
            {
                using Stream? inputStream = FileUtility.TryWaitOpenNoThrow(s_registryFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (inputStream == null)
                {
                    EdLog.Assets.Error("Failed to open file stream '{f}' for reading registry data", s_registryFile);
                    FileUtility.TryDelete(s_registryFile);
                    return;
                }

                using DataReader serializer = new DataReader(inputStream);

                if (serializer.ReadVersionHeader() != CurrentVersion)
                    throw new Exception("Invalid version in registry data");

                while (!serializer.IsAtEndOfStream)
                {
                    FileId assetId = (FileId)serializer.ReadGuid()!.Value;
                    DateTime lastFileWriteTime = serializer.ReadDateTime()!.Value;
                    DateTime lastDataWriteTime = serializer.ReadDateTime()!.Value;

                    serializer.ReadNewLine();

                    _physicalFiles[assetId] = new PhysicalFileInfo(lastFileWriteTime, lastDataWriteTime);
                }
            }
        }

        internal void SaveRegistryToDisk()
        {
            using Stream? outputStream = FileUtility.TryWaitOpenNoThrow(s_registryFile, FileMode.Create, FileAccess.Write, FileShare.None);
            if (outputStream == null)
            {
                EdLog.Assets.Error("Failed to open file stream '{f}' for writing registry data", s_registryFile);
                FileUtility.TryDelete(s_registryFile);
                return;
            }

            using DataWriter serializer = new DataWriter(outputStream);

            serializer.WriteVersionHeader(CurrentVersion);

            foreach (var (id, data) in _physicalFiles)
            {
                serializer.WriteValue(id);
                serializer.WriteValue(data.LastFileWriteTime);
                serializer.WriteValue(data.LastDataWriteTime);

                serializer.FinishLine();
            }
        }

        internal void StoreFileData(FileId id, string fullFilePath, string? fullDataPath)
        {
            DateTime lastWriteTimeFile = File.GetLastWriteTimeUtc(fullFilePath);
            DateTime lastWriteTimeData = fullDataPath == null ? PhysicalFileInfo.UninitializedDate : File.GetLastWriteTimeUtc(fullDataPath);

            PhysicalFileInfo fileInfo = _physicalFiles.GetOrAdd(id, PhysicalFileInfo.Uninitialized);
            PhysicalFileInfo newFileInfo = new PhysicalFileInfo(lastWriteTimeFile, fullDataPath == null ? fileInfo.LastDataWriteTime : lastWriteTimeData);

            _physicalFiles.TryUpdate(id, fileInfo, newFileInfo);
        }

        internal void RemoveFileFromRegistry(FileId id)
        {
            _physicalFiles.TryRemove(id, out _);
        }

        internal bool IsFileOutOfDate(FileId id, string fullFilePath)
        {
            PhysicalFileInfo fileInfo = _physicalFiles.GetOrAdd(id, PhysicalFileInfo.Uninitialized);
            DateTime lastWriteTime = File.GetLastWriteTimeUtc(fullFilePath);

            bool isFileUninitialized = fileInfo.LastFileWriteTime == PhysicalFileInfo.UninitializedDate;
            bool isFileOutOfDate = !isFileUninitialized && lastWriteTime > fileInfo.LastFileWriteTime;

            if (isFileUninitialized || isFileOutOfDate)
            {
                PhysicalFileInfo newFileInfo = new PhysicalFileInfo(lastWriteTime, fileInfo.LastDataWriteTime);
                _physicalFiles.TryUpdate(id, newFileInfo, fileInfo);

                return isFileOutOfDate;
            }

            return false;
        }

        internal bool IsDataOutOfDate(FileId id, string fullDataPath)
        {
            PhysicalFileInfo fileInfo = _physicalFiles.GetOrAdd(id, PhysicalFileInfo.Uninitialized);
            DateTime lastWriteTime = File.GetLastWriteTimeUtc(fullDataPath);

            bool isFileUninitialized = fileInfo.LastDataWriteTime == PhysicalFileInfo.UninitializedDate;
            bool isFileOutOfDate = !isFileUninitialized && lastWriteTime > fileInfo.LastDataWriteTime;

            if (isFileUninitialized || isFileOutOfDate)
            {
                PhysicalFileInfo newFileInfo = new PhysicalFileInfo(fileInfo.LastFileWriteTime, lastWriteTime);
                _physicalFiles.TryUpdate(id, newFileInfo, fileInfo);

                return isFileOutOfDate || isFileUninitialized;
            }

            return false;
        }

        internal bool IsFileOrDataOutOfDate(FileId id, string fullFilePath, string fullDataPath)
        {
            PhysicalFileInfo fileInfo = _physicalFiles.GetOrAdd(id, PhysicalFileInfo.Uninitialized);

            DateTime lastWriteTimeFile = File.GetLastWriteTimeUtc(fullFilePath);
            DateTime lastWriteTimeData = File.GetLastWriteTimeUtc(fullDataPath);

            bool isFileUninitialized = fileInfo.LastFileWriteTime == PhysicalFileInfo.UninitializedDate;
            bool isDataUninitialized = fileInfo.LastDataWriteTime == PhysicalFileInfo.UninitializedDate;

            bool isFileOutOfDate = !isFileUninitialized && lastWriteTimeFile > fileInfo.LastFileWriteTime;
            bool isDataOutOfDate = !isDataUninitialized && lastWriteTimeData > fileInfo.LastDataWriteTime;

            if (isFileUninitialized || isDataUninitialized || isFileOutOfDate || isDataOutOfDate)
            {
                PhysicalFileInfo newFileInfo = new PhysicalFileInfo(lastWriteTimeFile, lastWriteTimeData);
                _physicalFiles.TryUpdate(id, newFileInfo, fileInfo);

                return (isFileUninitialized || isFileOutOfDate) || (isDataUninitialized || isDataOutOfDate);
            }

            return false;
        }

        internal bool HasRegisteredFile(FileId id)
        {
            return _physicalFiles.ContainsKey(id);
        }

        public bool TryGetFileInfo(FileId id, [NotNullWhen(true)] out PhysicalFileInfo value)
        {
            return _physicalFiles.TryGetValue(id, out value);
        }

        private static string s_registryFile => Path.Combine(ProjectData.Instance!.Paths.LibrarySavedFolder, "PhysicalFiles.dat");

        public const int CurrentVersion = 1;
    }

    public readonly record struct PhysicalFileInfo(DateTime LastFileWriteTime, DateTime LastDataWriteTime) : IEquatable<PhysicalFileInfo>
    {
        public static PhysicalFileInfo Uninitialized => new PhysicalFileInfo(UninitializedDate, UninitializedDate);
        public static DateTime UninitializedDate => DateTime.MinValue;
    }
}
