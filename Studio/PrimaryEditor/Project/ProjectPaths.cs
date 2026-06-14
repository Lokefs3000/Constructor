using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Project
{
    public sealed class ProjectPaths
    {
        private string _libraryFolder;

        private string _librarySavedFolder;
        private string _libraryConfigFolder;
        private string _libraryImportedFolder;
        private string _libraryCacheFolder;

        internal ProjectPaths()
        {
            _libraryFolder = string.Empty;

            _librarySavedFolder = string.Empty;
            _libraryConfigFolder = string.Empty;
            _libraryImportedFolder = string.Empty;
            _libraryCacheFolder = string.Empty;
        }

        internal void SetupPaths(string projectDir)
        {
            _libraryFolder = Path.Combine(projectDir, "Library");

            _librarySavedFolder = Path.Combine(_libraryFolder, "Saved");
            _libraryConfigFolder = Path.Combine(_libraryFolder, "Config");
            _libraryImportedFolder = Path.Combine(_libraryFolder, "Imported");
            _libraryCacheFolder = Path.Combine(_libraryFolder, "Cache");
        }

        public string LibraryFolder => _libraryFolder;

        public string LibrarySavedFolder => _librarySavedFolder;
        public string LibraryConfigFolder => _libraryConfigFolder;
        public string LibraryImportedFolder => _libraryImportedFolder;
        public string LibraryCacheFolder => _libraryCacheFolder;
    }
}
