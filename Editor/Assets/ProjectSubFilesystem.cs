using Primary.Assets;
using Primary.Common;
using Primary.Common.Streams;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Assets
{
    internal class ProjectSubFilesystem : ISubFilesystem
    {
        private string _namespace;
        private string _absolutePath;

        private bool _mappingsModified;

        private ConcurrentDictionary<string, string> _fileRemappings;

        public ProjectSubFilesystem(string filepath)
        {
            string absolute = Path.GetFullPath(filepath);

            _namespace = Path.GetFileNameWithoutExtension(absolute)!;
            _absolutePath = Path.GetDirectoryName(absolute)!;

            _mappingsModified = false;

            _fileRemappings = new ConcurrentDictionary<string, string>();

            string mappingFile = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "FileRemappings.dat");
            if (File.Exists(mappingFile))
            {
                ReadFileMappings(mappingFile);
            }
        }

        public void Dispose()
        {
            FlushFileRemappings();
        }

        private void ReadFileMappings(string mappingsFilePath)
        {
            string source = File.ReadAllText(mappingsFilePath);

            int i = 0;
            while (i < source.Length)
            {
                int find = source.IndexOf(';', i);
                if (find == -1)
                {
                    i = source.IndexOf('\n', i + 1);
                    if (i == -1)
                        break;
                }

                string remapFile = source.Substring(i, find - i);

                int j = find + 1;
                while (j < source.Length && !char.IsControl(source[j]))
                    j++;

                string realFile = source.Substring(find + 1, j - 1 - find);

                string fullRemapPath = Path.Combine(Editor.GlobalSingleton.ProjectPath, remapFile);
                string fullRealPath = Path.Combine(Editor.GlobalSingleton.ProjectPath, realFile);

                if (File.Exists(fullRemapPath) && File.Exists(fullRealPath))
                {
                    ExceptionUtility.Assert(_fileRemappings.TryAdd(remapFile, realFile));
                }

                i = source.IndexOf('\n', i + 1);
                if (i == -1)
                    break;
                i++;
            }
        }

        internal void RemapFile(string filePath, string remapPath)
        {
            filePath = filePath.Replace('\\', '/');
            remapPath = remapPath.Replace('\\', '/');

            _fileRemappings.AddOrUpdate(filePath, remapPath, (_, _) => remapPath);
            _mappingsModified = true;
        }

        internal void FlushFileRemappings()
        {
            string outputFile = Path.Combine(EditorFilepaths.LibraryIntermediatePath, "FileRemappings.dat");

            StringBuilder sb = new StringBuilder();
            foreach (var kvp in _fileRemappings)
            {
                sb.Append(kvp.Key);
                sb.Append(';');
                sb.AppendLine(kvp.Value);
            }

            File.WriteAllText(outputFile, sb.ToString());
        }

        public string ReadString(ReadOnlySpan<char> path)
        {
            if (!path.StartsWith(_namespace))
            {
                throw new ArgumentException("Incorrect file namespace!");
            }

            string absolutePath;
            string localPath = path.ToString();

            if (_fileRemappings.TryGetValue(localPath, out string? remap))
                absolutePath = Path.Combine(_absolutePath, remap);
            else
                absolutePath = Path.Combine(_absolutePath, localPath);

            return File.ReadAllText(absolutePath);
        }

        public Stream OpenStream(ReadOnlySpan<char> path)
        {
            if (!path.StartsWith(_namespace))
            {
                throw new ArgumentException("Incorrect file namespace!");
            }

            string absolutePath = string.Empty;
            string localPath = path.ToString();

            AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;
            if (_fileRemappings.TryGetValue(localPath, out string? remap))
            {
                if (pipeline.IsImportingAsset(path))
                {
                    EdLog.Assets.Information("Waiting on file import: {lc}", path.ToString());
                    pipeline.ImportChangesOrGetRunning(path)?.Wait();
                }

                absolutePath = Path.Combine(_absolutePath, remap);
            }
            else
            {
                bool hasNewRemap = false;
                if (!pipeline.IsAssetUpToDate(localPath))
                {
                    if (pipeline.CanBeImported(path))
                    {
                        EdLog.Assets.Information("Waiting on new file import: {lc}", path.ToString());
                        Task? task = pipeline.ImportChangesOrGetRunning(path);
                        task?.Wait();

                        if (_fileRemappings.TryGetValue(localPath, out remap))
                        {
                            absolutePath = Path.Combine(_absolutePath, remap);
                            hasNewRemap = true;
                        }
                    }
                }

                if (!hasNewRemap)
                    absolutePath = Path.Combine(_absolutePath, localPath);
            }

            return FileUtility.TryWaitOpen(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public bool Exists(ReadOnlySpan<char> path)
        {
            if (path.StartsWith(_namespace))
                return File.Exists(Path.Combine(_absolutePath, path.ToString()));

            return false;
        }

        private class ProjectFileStream : Stream
        {
            private FileStream _stream;

            private long _basePosition;
            private long _length;

            public override bool CanRead => _stream.CanRead;
            public override bool CanSeek => _stream.CanSeek;
            public override bool CanWrite => _stream.CanWrite;

            public override long Length => _length;
            public override long Position { get => _stream.Position - _basePosition; set => _stream.Position = value + _basePosition; }

            internal ProjectFileStream(FileStream stream, long basePosition, long length)
            {
                _stream = stream;
                _basePosition = basePosition;
                _length = length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override void Dispose(bool disposing) => _stream.Dispose();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Flush() => _stream.Flush();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override long Seek(long offset, SeekOrigin origin)
            {
                switch (origin)
                {
                    case SeekOrigin.Current: return _stream.Seek(offset, origin);
                    default: return _stream.Seek(offset + _basePosition, origin);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void SetLength(long value) => _stream.SetLength(value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
        }
    }
}
