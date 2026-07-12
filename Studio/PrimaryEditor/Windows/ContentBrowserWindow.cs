using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Input;
using EditorUI.Mathematics;
using EditorUI.Serialization;
using EditorUI.Text;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Input.Devices;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Filesystem;
using PrimaryEditor.Core;
using PrimaryEditor.Windows.ContentBrowser;

namespace PrimaryEditor.Windows
{
    public sealed class ContentBrowserWindow : EditorWindow
    {
        private TreeView? _directoryTree;
        private LayoutFrame? _contentToolbar;
        private GridView? _contentGrid;

        private EntryGridCollection _gridCollection;
        private EntryGridItemStyle? _gridItemStyle;

        private List<FilesystemDirectory> _directoryStack;
        private List<Label> _directoryPathPool;

        private Dictionary<string, DirectoryTreeNode> _treeNodes;

        public ContentBrowserWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            _gridCollection = new EntryGridCollection(this);
            _gridItemStyle = null;

            _directoryStack = new List<FilesystemDirectory>();
            _directoryPathPool = new List<Label>();

            _treeNodes = new Dictionary<string, DirectoryTreeNode>();

            CreateDefaultNodes();
            LoadLayout("Editor/UI/ContentBrowser.layout");
        }

        protected override void DestroySelf()
        {
            foreach (Label label in _directoryPathPool)
            {
                label.Destroy();
            }

            _directoryPathPool.Clear();

            base.DestroySelf();
        }

        protected internal override void InitializeSelf()
        {
            _directoryTree = RootWidget.FindWidgetWithId<TreeView>("directory-tree", true)!;
            _contentToolbar = RootWidget.FindWidgetWithId<LayoutFrame>("content-toolbar", true)!;
            _contentGrid = RootWidget.FindWidgetWithId<GridView>("content-grid", true)!;

            {
                FilesystemManager filesystemManager = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
                foreach (BaseFilesystem filesystem in filesystemManager.Filesystems)
                {
                    if (filesystem is not ContentFilesystem content)
                        continue;

                    DirectoryTreeNode treeNode = CreateTreeNodesFrom(content.Structure.RootDirectory);
                    _directoryTree.AddNode(treeNode);
                }
            }

            {
                foreach (Label label in _directoryPathPool)
                {
                    if (label.Text != null)
                        label.Parent = _contentToolbar;
                    else
                        break;
                }
            }

            {
                _gridItemStyle = new EntryGridItemStyle(_contentGrid);
                _gridCollection.ItemStyle = _gridItemStyle;

                _contentGrid.Items = _gridCollection;
                _contentGrid.ItemStyle = _gridItemStyle;
            }

            if (_directoryStack.Count == 0)
            {
                ScopeToDirectory("Content");
            }
        }

        protected internal override void CleanupReloadSelf()
        {
            _directoryTree?.ClearNodes();

            _directoryTree = null;
            _contentToolbar = null;
            _contentGrid = null;

            _gridItemStyle = null;

            foreach (Label label in _directoryPathPool)
            {
                label.Parent = null;
            }
        }

        public void ScopeToDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            FilesystemManager filesystemManager = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            if (!filesystemManager.TryFindFilesystemFor(path, out BaseFilesystem? value) || value is not ContentFilesystem content)
                return;

            FilesystemStructure structure = content.Structure;
            if (!path.Contains('/') || path.Length == content.NamespaceKey.Length + 1)
            {
                _directoryStack.Clear();
                _directoryStack.Add(structure.RootDirectory);

                _gridCollection.CurrentDirectory = structure.RootDirectory;
                UpdateCurrentPathLabels();
                return;
            }

            FilesystemDirectory rootDir = _directoryStack[0];
            if (rootDir != structure.RootDirectory)
            {
                _directoryStack.Clear();

                FilesystemDirectory? currentDir = null;
                ReadOnlySpan<char> currentPath = path;

                while (!currentPath.IsEmpty)
                {
                    if (structure.TryGetDirectory(currentPath, out currentDir))
                    {
                        do
                        {
                            _directoryStack.Insert(0, currentDir);
                        } while ((currentDir = currentDir.Parent) != null);

                        _gridCollection.CurrentDirectory = _directoryStack[^1];
                        UpdateCurrentPathLabels();
                        return;
                    }
                    else
                    {
                        int lastIndexOf = currentPath.LastIndexOf('/');
                        if (lastIndexOf == -1)
                        {
                            _gridCollection.CurrentDirectory = structure.RootDirectory;
                            _directoryStack.Add(structure.RootDirectory);
                            UpdateCurrentPathLabels();
                            return;
                        }

                        currentPath = currentPath[..lastIndexOf];
                    }
                }

                throw new UnreachableException();
            }
            else
            {
                ReadOnlySpan<char> currentName = path.AsSpan()[..path.LastIndexOf('/')];
                
                // pop all directories above that we don't care about
                for (int i = _directoryStack.Count - 1; i > 0; --i)
                {
                    if (!currentName.SequenceEqual(_directoryStack[i].LocalPath))
                        _directoryStack.RemoveAt(i);
                    else
                        break;
                }

                FilesystemDirectory currentDir = _directoryStack[^1];
                if (path == currentDir.LocalPath)
                {
                    _gridCollection.CurrentDirectory = currentDir;
                    UpdateCurrentPathLabels();
                    return;
                }

                currentName = path.AsSpan()[(currentDir.LocalPath.Length + 1)..];
                foreach (var token in currentName.Tokenize('/'))
                {
                    if (currentDir.TryGetDirectory(token.ToString(), out FilesystemDirectory? subDir))
                    {
                        _directoryStack.Add(subDir);
                    }
                    else
                    {
                        break;
                    }
                }

                _gridCollection.CurrentDirectory = _directoryStack[^1];
                UpdateCurrentPathLabels();
            }
        }

        private void UpdateCurrentPathLabels()
        {
            for (int i = 0; i < _directoryStack.Count; ++i)
            {
                FilesystemDirectory directory = _directoryStack[i];

                Label label;
                if (i >= _directoryPathPool.Count)
                {
                    label = new Label()
                    {
                        Size = new UIValue2(0.0f, 1.0f),
                        Alignment = TextAlignment.CenterLeft,
                    };

                    label.OnMousePress += OnDirectoryLabelPressed;

                    _directoryPathPool.Add(label);
                }
                else
                {
                    label = _directoryPathPool[i];
                }

                if (label.Text != directory.Name)
                {
                    label.Text = directory.Name;
                    label.Parent = _contentToolbar;
                }
            }

            for (int i = _directoryStack.Count; i < _directoryPathPool.Count; ++i)
            {
                Label label = _directoryPathPool[i];
                if (label.Text != null)
                {
                    label.Text = null;
                    label.Parent = null;
                }
            }
        }

        private void CreateDefaultNodes()
        {
            FilesystemManager filesystemManager = EditorRuntime.Instance.AssetPipeline.FilesystemManager;
            foreach (BaseFilesystem filesystem in filesystemManager.Filesystems)
            {
                if (filesystem is not ContentFilesystem content)
                    continue;

                CreateTreeNodesFrom(content.Structure.RootDirectory);
            }
        }

        private DirectoryTreeNode CreateTreeNodesFrom(FilesystemDirectory directory)
        {
            if (_treeNodes.TryGetValue(directory.LocalPath, out DirectoryTreeNode? treeNode))
                return treeNode;

            treeNode = new DirectoryTreeNode(this, directory);

            foreach (var (entry, info) in directory.Entries)
            {
                if (!entry.IsFile)
                {
                    treeNode.AddNode(CreateTreeNodesFrom((FilesystemDirectory)info));
                }
            }

            _treeNodes.Add(directory.LocalPath, treeNode);
            return treeNode;
        }

        private void OnDirectoryLabelPressed(Widget widget, UIMouseInputEvent inputEvent)
        {
            Label label = (Label)widget;
            int indexOf = _directoryPathPool.IndexOf(label);

            if (indexOf != -1 && indexOf + 1 < _directoryStack.Count)
            {
                _directoryStack.RemoveRange(indexOf + 1, _directoryStack.Count - (indexOf + 1));

                _gridCollection.CurrentDirectory = _directoryStack[^1];
                UpdateCurrentPathLabels();
            }
        }
    }
}
