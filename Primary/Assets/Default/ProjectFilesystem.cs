using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Primary.Assets.Types;

namespace Primary.Assets.Default
{
    public sealed class ProjectFilesystem : ISubFilesystem
    {
        private readonly string _directory;
        private readonly string _key;

        private readonly string _contentFolder;

        private readonly FileMappingTable? _mappingTable;

        public ProjectFilesystem(string directory, string key, FileMappingTable? mappingTable)
        {
            _directory = directory;
            _key = key;

            _contentFolder = Path.Combine(directory, key);

            _mappingTable = mappingTable;
        }

        public void Dispose()
        {
        }

        public bool Exists(ReadOnlySpan<char> path)
        {
            if (!path.StartsWith(_key, StringComparison.Ordinal))
                return false;
            return File.Exists(ResolveFilePath(path));
        }

        public Stream? OpenStream(ReadOnlySpan<char> path)
        {
            if (!path.StartsWith(_key, StringComparison.Ordinal))
                return null;
            return File.OpenRead(ResolveFilePath(path));
        }

        public string? ReadAllText(ReadOnlySpan<char> path)
        {
            if (!path.StartsWith(_key, StringComparison.Ordinal))
                return null;
            return File.ReadAllText(ResolveFilePath(path));
        }

        private string ResolveFilePath(ReadOnlySpan<char> path)
        {
            if (_mappingTable != null && _mappingTable.TryGetFileMapping(path, out string? mapping))
                return mapping;
            else
                return Path.Combine(_contentFolder, path[(_key.Length + 1)..].ToString());
        }
    }
}
