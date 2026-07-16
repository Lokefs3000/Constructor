using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Input;
using EditorUI.Visual;
using EditorUI.Widgets.Components;
using Primary.Assets;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using PrimaryEditor.Assets.Filesystem;

namespace PrimaryEditor.Windows.ContentBrowser
{
    internal sealed class DirectoryTreeNode : TreeNode
    {
        private readonly ContentBrowserWindow _window;
        private readonly string _localPath;

        private readonly Sprite? _folderClosedIcon;
        private readonly Sprite? _folderOpenIcon;

        internal DirectoryTreeNode(ContentBrowserWindow window, FilesystemDirectory info)
        {
            _window = window;
            _localPath = info.LocalPath;

            Text = info.Name;

            TextureAtlasAsset textureAtlas = AssetManager.LoadAsset<TextureAtlasAsset>("Editor/UI/Icons/DefaultFileIcons.atlas").WaitIfNotLoaded();
            _folderClosedIcon = textureAtlas.TryFindSpriteOrNull("FolderClosed20");
            _folderOpenIcon = textureAtlas.TryFindSpriteOrNull("FolderOpen20");
        }

        protected override void PaintSelf(ref readonly PainterContext painter, Vector2 availableSize)
        {
            painter.AddImage(new Boundaries(Vector2.Zero, new Vector2(availableSize.Y)), (IsExpanded || ChildNodes.Count == 0) ? _folderOpenIcon : _folderClosedIcon, new Paint(Color.White));

            painter.PushTranslate(new Vector2(availableSize.Y, 0.0f));

            availableSize.X -= availableSize.Y;
            base.PaintSelf(in painter, availableSize);

            painter.PopTranslate();
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            if (inputEvent.EventType == UIInputEventType.MouseDown && inputEvent.Mouse.Button == MouseButton.Left && !InputSystem.Keyboard.KeyModifiers.HasAny(KeyModifier.Control))
            {
                _window.ScopeToDirectory(_localPath);
            }

            return base.HandleEventSelf(in inputEvent);
        }

        public string LocalPath => _localPath;
    }
}
