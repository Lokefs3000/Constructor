using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Primary.Collections.ReadOnly;

namespace PrimaryEditor.Assets.Filesystem
{
    public sealed class FilesystemStructure
    {
        private FilesystemDirectory _rootDirectory;
        private Dictionary<string, FilesystemDirectory> _directories;

        private Dictionary<string, FilesystemDirectory>.AlternateLookup<ReadOnlySpan<char>> _directoriesAlt;

        internal FilesystemStructure(string rootFolderName)
        {
            _rootDirectory = new FilesystemDirectory(rootFolderName, rootFolderName, null);
            _directories = new Dictionary<string, FilesystemDirectory> { { rootFolderName, _rootDirectory } };

            _directoriesAlt = _directories.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        public bool TryGetDirectory(ReadOnlySpan<char> localPath, [NotNullWhen(true)] out FilesystemDirectory? value)
        {
            return _directoriesAlt.TryGetValue(localPath, out value);
        }

        internal FilesystemDirectory? AddDirectory(string localPath)
        {
            if (_directories.TryGetValue(localPath, out FilesystemDirectory? directory))
                return directory;

            FilesystemDirectory? parentDir = FindParentDirectoryFor(localPath);
            if (parentDir == null)
                return null;

            directory = new FilesystemDirectory(Path.GetFileNameWithoutExtension(localPath), localPath, parentDir);

            parentDir.AddDirectory(directory.Name, directory);
            _directories.Add(localPath, directory);

            return directory;
        }

        internal void RemoveDirectory(string localPath)
        {
            if (_directories.Remove(localPath, out FilesystemDirectory? dir))
            {
                dir.Parent?.RemoveEntry(Path.GetFileNameWithoutExtension(localPath), false);
            }
        }

        internal void AddFile(string localPath)
        {
            FilesystemDirectory? parentDir = FindParentDirectoryFor(localPath);
            if (parentDir == null)
                return;

            string name = Path.GetFileName(localPath);

            if (parentDir.HasEntry(name, true))
                return;

            parentDir.AddFile(name, new FilesystemFile(name, localPath));
        }

        internal void RemoveFile(string localPath)
        {
            FilesystemDirectory? parentDir = FindParentDirectoryFor(localPath, false);
            if (parentDir == null)
                return;

            string name = Path.GetFileName(localPath);
            parentDir.RemoveEntry(name, false);
        }

        private FilesystemDirectory? FindParentDirectoryFor(ReadOnlySpan<char> localPath, bool createIfNull = true)
        {
            int index = localPath.LastIndexOf('/');
            if (index == -1)
                return null;

            localPath = localPath[..index];

            if (_directoriesAlt.TryGetValue(localPath, out FilesystemDirectory? dir))
            {
                return dir;
            }
            else if (createIfNull)
            {
                return AddDirectory(localPath.ToString());
            }
            else
            {
                return null;
            }
        }

        public FilesystemDirectory RootDirectory => _rootDirectory;
    }

    public sealed class FilesystemDirectory
    {
        private string _name;
        private string _localPath;
        private FilesystemDirectory? _parent;

        private SortedList<FilesystemEntry, object>? _entries;

        internal FilesystemDirectory(string name, string localPath, FilesystemDirectory? parentDir)
        {
            _name = name;
            _localPath = localPath;
            _parent = parentDir;

            _entries = null;
        }

        public bool TryGetDirectory(string name, [NotNullWhen(true)] out FilesystemDirectory? value)
        {
            if (_entries == null)
            {
                value = null;
                return false;
            }

            bool r = _entries.TryGetValue(new FilesystemEntry(name, false), out object? obj);
            return (value = obj as FilesystemDirectory) != null && r;
        }

        public bool TryGetFile(string name, [NotNullWhen(true)] out FilesystemFile? value)
        {
            if (_entries == null)
            {
                value = null;
                return false;
            }

            bool r = _entries.TryGetValue(new FilesystemEntry(name, true), out object? obj);
            return (value = obj as FilesystemFile) != null && r;
        }

        internal void AddDirectory(string name, FilesystemDirectory dir)
        {
            if (_entries == null)
                _entries = new SortedList<FilesystemEntry, object> { { new FilesystemEntry(name, false), dir } };
            else
                _entries.Add(new FilesystemEntry(name, false), dir);
        }

        internal void AddFile(string name, FilesystemFile file)
        {
            if (_entries == null)
                _entries = new SortedList<FilesystemEntry, object> { { new FilesystemEntry(name, true), file } };
            else
                _entries.Add(new FilesystemEntry(name, true), file);
        }

        internal void RemoveEntry(string name, bool isFile)
        {
            if (_entries != null && _entries.Remove(new FilesystemEntry(name, isFile)) && _entries.Count == 0)
            {
                _entries = null;
            }
        }

        internal bool HasEntry(string name, bool isFile) => _entries?.ContainsKey(new FilesystemEntry(name, isFile)) ?? false;

        public string Name => _name;
        public string LocalPath => _localPath;
        public FilesystemDirectory? Parent => _parent;

        public ROSortedList<FilesystemEntry, object> Entries => _entries ?? ROSortedList<FilesystemEntry, object>.Empty;

    }

    public record class FilesystemFile(string Name, string LocalPath);

    public readonly record struct FilesystemEntry(string Name, bool IsFile) : IEquatable<FilesystemEntry>, IComparable<FilesystemEntry>
    {
        public int CompareTo(FilesystemEntry other)
        {
            int r = other.IsFile.CompareTo(IsFile);
            return r == 0 ? Name.CompareTo(other.Name, StringComparison.Ordinal) : -r;
        }
    }
}
