using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using PrimaryEditor.Assets.Importers;

namespace PrimaryEditor.Assets
{
    public sealed class ImporterRegistry
    {
        private Dictionary<string, IAssetImporter> _importers;
        private Dictionary<string, IAssetImporter>.AlternateLookup<ReadOnlySpan<char>> _altImporterLookup;

        internal ImporterRegistry()
        {
            _importers = new Dictionary<string, IAssetImporter>();
            _altImporterLookup = _importers.GetAlternateLookup<ReadOnlySpan<char>>();
        }

        public bool TryGetImporterFor(ReadOnlySpan<char> extension, [NotNullWhen(true)] out IAssetImporter? importer)
        {
            return _altImporterLookup.TryGetValue(extension, out importer);
        }

        public bool TryGetImporterForPath(ReadOnlySpan<char> path, [NotNullWhen(true)] out IAssetImporter? importer)
        {
            int index = path.LastIndexOf('.');
            if (index == -1)
            {
                importer = null;
                return false;
            }

            return _altImporterLookup.TryGetValue(path[index..], out importer);
        }

        internal void RegisterImporter<T>(params string[] extensions) where T : class, IAssetImporter, new()
        {
            IAssetImporter importer = new T();

            foreach (string extension in extensions)
            {
                if (!_importers.TryAdd(extension, importer))
                {
                    EdLog.Assets.Warning("Cannot register asset importer '{t}' because a diffent importer was already registered to the file extension '{ex}'", typeof(T), extension);
                }
            }
        }
    }
}
