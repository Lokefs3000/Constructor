using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Common;

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

        internal void RenameDirectory(string oldLocalPath, string newLocalPath)
        {
            if (_directories.TryGetValue(oldLocalPath, out FilesystemDirectory? directory))
            {
                _directories.Remove(oldLocalPath);
                _directories.Add(newLocalPath, directory);

                directory.Name = Path.GetDirectoryName(newLocalPath) ?? string.Empty;
                directory.LocalPath = newLocalPath;

                if (directory.Parent != null)
                {
                    FilesystemDirectory parentDir = directory.Parent;
                    parentDir.RemoveEntry(directory.Name, false);
                    parentDir.AddDirectory(directory.Name, directory);
                }

                RenameEntriesInDirectory(directory, oldLocalPath, newLocalPath);
            }
        }

        internal void RenameFile(string oldLocalPath, string newLocalPath)
        {
            FilesystemDirectory? parentDir = FindParentDirectoryFor(oldLocalPath, false);
            if (parentDir != null)
            {
                string fileName = Path.GetFileName(oldLocalPath);
                FilesystemFile newFile = new FilesystemFile(Path.GetFileName(newLocalPath), newLocalPath);

                parentDir.RemoveEntry(fileName, true);
                parentDir.AddFile(newFile.Name, newFile);
            }
        }

        private void RenameEntriesInDirectory(FilesystemDirectory directory, string oldLocalPath, string newLocalPath)
        {
            if (directory.Entries.Count > 0)
            {
                using RentedArray<KeyValuePair<FilesystemEntry, object>> entries = new RentedArray<KeyValuePair<FilesystemEntry, object>>(directory.Entries.Count);

                int index = 0;
                foreach (var (entry, file) in directory.Entries)
                {
                    entries[index++] = new KeyValuePair<FilesystemEntry, object>(entry, file);
                }

                if (index > 0)
                {
                    directory.ClearEntries();

                    for (int i = 0; i < index; ++i)
                    {
                        KeyValuePair<FilesystemEntry, object> kvp = entries[i];
                        
                        if (kvp.Key.IsFile)
                        {
                            string updatedLocalPath = string.Concat(newLocalPath, ((FilesystemFile)kvp.Value).LocalPath.AsSpan(oldLocalPath.Length));
                            directory.AddFile(kvp.Key.Name, new FilesystemFile(kvp.Key.Name, updatedLocalPath));
                        }
                        else
                        {
                            FilesystemDirectory dir = (FilesystemDirectory)kvp.Value;
                            string updatedLocalPath = string.Concat(newLocalPath, dir.LocalPath.AsSpan(oldLocalPath.Length));

                            _directories.Remove(dir.LocalPath);
                            _directories.Add(updatedLocalPath, dir);

                            dir.LocalPath = updatedLocalPath;
                            directory.AddDirectory(kvp.Key.Name, dir);

                            if (dir.Entries.Count > 0)
                            {
                                RenameEntriesInDirectory(dir, oldLocalPath, newLocalPath);
                            }
                        }
                    }
                }
            }
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

        internal void ClearEntries()
        {
            _entries?.Clear();
            _entries = null;
        }

        internal bool HasEntry(string name, bool isFile) => _entries?.ContainsKey(new FilesystemEntry(name, isFile)) ?? false;

        public string Name { get => _name; internal set => _name = value; }
        public string LocalPath { get => _localPath; internal set => _localPath = value; }
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
