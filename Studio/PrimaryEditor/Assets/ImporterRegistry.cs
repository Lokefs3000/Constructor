using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Assets.Importers;

namespace PrimaryEditor.Assets
{
    public sealed class ImporterRegistry
    {
        private Dictionary<string, IAssetImporter> _importers;

        internal ImporterRegistry()
        {
            _importers = new Dictionary<string, IAssetImporter>();
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
