using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Assets
{
    public sealed class ProjectSubFilesystem : ISubFilesystem
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

            if (!_absolutePath.EndsWith(Path.PathSeparator))
                _absolutePath += Path.DirectorySeparatorChar;

            _mappingsModified = false;

            _fileRemappings = new ConcurrentDictionary<string, string>();

            string mappingFile = FileRemappingsFile;
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

                string fullRemapPath = Path.Combine(_absolutePath, remapFile);
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
            string outputFile = FileRemappingsFile;

            StringBuilder sb = new StringBuilder();
            foreach (var kvp in _fileRemappings)
            {
                sb.Append(kvp.Key);
                sb.Append(';');
                sb.AppendLine(kvp.Value);
            }

            File.WriteAllText(outputFile, sb.ToString());
        }

        /// <summary>Thread-safe</summary>
        public string GetFullPath(string path)
        {
            return Path.Combine(_absolutePath, path);
        }

        /// <summary>Thread-safe</summary>
        public string? ReadString(ReadOnlySpan<char> path)
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

            try
            {
                return File.ReadAllText(absolutePath);
            }
            catch (Exception)
            {

            }

            return null;
        }

        /// <summary>Thread-safe</summary>
        public Stream? OpenStream(ReadOnlySpan<char> path)
        {
            if (!path.StartsWith(_namespace))
            {
                return null;
            }

            //temporary (yeah this will prob stay a while cause its temporary lmao)
            const bool waitIfImportPending = true;

            string absolutePath = string.Empty;
            string localPath = path.ToString();

            AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;
            if (_fileRemappings.TryGetValue(localPath, out string? remap))
            {
                if (pipeline != null && pipeline.IsImportingAsset(path))
                {
                    if (waitIfImportPending)
                    {
                        EdLog.Assets.Information("Waiting on file import: {lc}", path.ToString());

                        Task? task = pipeline.ImportChangesOrGetRunning(path);
                        task?.Wait(2000);
                    }
                }

                absolutePath = Path.Combine(Editor.GlobalSingleton.ProjectPath, remap);
            }
            else if (pipeline != null)
            {
                bool hasNewRemap = false;
                if (!pipeline.IsAssetUpToDate(localPath))
                {
                    if (pipeline.CanBeImported(path))
                    {
                        if (waitIfImportPending)
                        {
                            Task? task = pipeline.ImportChangesOrGetRunning(path);
                            task?.Wait(2000);
                        }

                        if (_fileRemappings.TryGetValue(localPath, out remap))
                        {
                            absolutePath = Path.Combine(Editor.GlobalSingleton.ProjectPath, remap);
                            hasNewRemap = true;
                        }
                    }
                }

                if (!hasNewRemap)
                    absolutePath = Path.Combine(_absolutePath, localPath);
            }
            else
                absolutePath = Path.Combine(_absolutePath, localPath);

            return FileUtility.TryWaitOpenNoThrow(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>Thread-safe</summary>
        public bool Exists(ReadOnlySpan<char> path)
        {
            if (path.StartsWith(_namespace))
                return File.Exists(Path.Combine(_absolutePath, path.ToString()));

            return false;
        }

        public string FileRemappingsFile => Path.Combine(EditorFilepaths.LibraryIntermediatePath, $"FileRemappings_{(uint)Path.Combine(_absolutePath, _namespace).GetDjb2HashCode()}.dat");

        public ref readonly string Namespace => ref _namespace;
        public ref readonly string AbsolutePath => ref _absolutePath;

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
