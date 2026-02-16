using Editor.Assets;
using Editor.Storage;
using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Elements.Tree;
using Editor.UI.Serialization;
using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Windows
{
    internal sealed class AssetBrowser : UIWindow
    {
        private UIFontAsset _font;

        private UITreeView _folderView;

        public AssetBrowser(int uniqueWindowId) : base(uniqueWindowId)
        {
            WindowTitle = "Asset browser";

            string? xml = AssetFilesystem.ReadString("Editor/UI/AssetBrowser.ui");
            if (xml != null)
            {
                UISerializer.TryDeserialize(xml, RootElement);
            }

            _font = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont");

            _folderView = RootElement.FindElementWithId<UITreeView>("FileTreeView")!;

            CreateInitialDirectories();
        }

        private void CreateInitialDirectories()
        {
            AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;
            AssetFilesystemWatcher watcher = pipeline.ContentWatcher;

            UIFontStyle style = _font.FindStyle(null)!;

            lock (watcher.LockableTree)
            {
                if (watcher.GetDirectory("Content", out AssetDirectory dir))
                    CreateForDirectory("Content", "Content", dir, _folderView.RootNode);

                void CreateForDirectory(string fullPath, string name, AssetDirectory directory, BaseTreeNode parentNode)
                {
                    TreeNode node = new TreeNode
                    {
                        Parent = parentNode,
                        Text = name,
                        FontStyle = style
                    };

                    foreach (string subDirectory in directory.Subdirectories)
                    {
                        if (watcher.GetDirectory($"{fullPath}/{subDirectory}", out AssetDirectory subDir))
                            CreateForDirectory($"{fullPath}/{subDirectory}", subDirectory, subDir, node);
                    }
                }
            }
        }
    }
}
