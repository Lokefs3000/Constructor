using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Binding;
using EditorUI.Widgets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Assets.Importers;
using PrimaryEditor.Core;
using PrimaryEditor.Inspector.Contexts.Texture;

namespace PrimaryEditor.Windows.ContentBrowser
{
    internal sealed class EntryGridCollection : ICollectionBinding<GridViewItem>
    {
        private readonly ContentBrowserWindow _window;

        private EntryGridItemStyle? _itemStyle;

        private FileGridItem _fileGridItem;
        private DirectoryGridItem _directoryGridItem;

        private FilesystemDirectory? _currentDirectory;
        private HashSet<object> _currentlySelected;

        internal EntryGridCollection(ContentBrowserWindow window)
        {
            _window = window;

            _itemStyle = null;

            _fileGridItem = new FileGridItem(this);
            _directoryGridItem = new DirectoryGridItem(this);

            _currentDirectory = null;
            _currentlySelected = new HashSet<object>();
        }

        public GridViewItem GetItemAt(int index)
        {
            FilesystemEntry entry = _currentDirectory!.Entries.Keys[index];
            object obj = _currentDirectory!.Entries.Values[index];

            if (entry.IsFile)
            {
                _fileGridItem.File = obj as FilesystemFile;
                return _fileGridItem;
            }
            else
            {
                _directoryGridItem.Directory = obj as FilesystemDirectory;
                return _directoryGridItem;
            }
        }

        internal void Select(object obj)
        {
            if (!InputSystem.Keyboard.KeyModifiers.HasAny(KeyModifier.Control))
            {
                if (_currentlySelected.Count == 1 && _currentlySelected.Contains(obj))
                    return;
                _currentlySelected.Clear();
            }

            if (_currentlySelected.Add(obj))
            {
                if (obj is FilesystemFile file)
                {
                    AssetPipeline pipeline = EditorRuntime.Instance.AssetPipeline;
                    if (pipeline.ImporterRegistry.TryGetImporterForPath(file.LocalPath, out AssetImporterData importerData) &&
                        pipeline.AssetRegistry.TryLookupIdForPath(file.LocalPath, out FileId assetId))
                    {
                        if (importerData.Importer is TextureImporter)
                        {
                            AssetId id = new AssetId(assetId, AssetId.NoLocalId);
                            EditorRuntime.Instance.InspectorManager.StartInspect<TextureInspectorContext, AssetId>(ref id);
                        }
                    }
                }
            }
        }

        internal void ScopeTo(object obj)
        {
            if (!InputSystem.Keyboard.KeyModifiers.HasAny(KeyModifier.Control))
            {
                if (obj is FilesystemDirectory directory)
                    _window.ScopeToDirectory(directory.LocalPath);
                else if (obj is FilesystemFile file)
                {

                }
            }
        }

        internal bool IsSelected(object obj) => _currentlySelected.Contains(obj);

        internal EntryGridItemStyle? ItemStyle { get => _itemStyle; set => _itemStyle = value; }

        public FilesystemDirectory? CurrentDirectory { get => _currentDirectory; set => _currentDirectory = value; }

        public int Count => _currentDirectory?.Entries.Count ?? 0;
    }
}
