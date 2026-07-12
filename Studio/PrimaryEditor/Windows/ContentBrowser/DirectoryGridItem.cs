using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI;
using EditorUI.Built;
using EditorUI.Input;
using EditorUI.Text;
using EditorUI.Visual;
using EditorUI.Widgets;
using Primary.Assets;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;
using PrimaryEditor.Assets.Filesystem;

namespace PrimaryEditor.Windows.ContentBrowser
{
    internal sealed class DirectoryGridItem : GridViewItem
    {
        private readonly EntryGridCollection _collection;

        private readonly Sprite? _folderClosedIcon;
        private readonly Sprite? _folderOpenIcon;

        private FilesystemDirectory? _directory;

        internal DirectoryGridItem(EntryGridCollection collection)
        {
            _collection = collection;

            TextureAtlasAsset textureAtlas = AssetManager.LoadAsset<TextureAtlasAsset>("Editor/UI/Icons/DefaultFileIcons.atlas").WaitIfNotLoaded();
            _folderClosedIcon = textureAtlas.TryFindSpriteOrNull("FolderClosed");
            _folderOpenIcon = textureAtlas.TryFindSpriteOrNull("FolderOpen");

            _directory = null;
        }

        public override void OnPaint(GridView gridView, ref readonly PainterContext painter, Vector2 itemSize)
        {
            if (_directory != null && _collection.ItemStyle != null)
            {
                if (_collection.IsSelected(_directory))
                    painter.AddRectangle(new Boundaries(Vector2.Zero, itemSize), new Paint(_collection.ItemStyle.SelectedColor), new Vector4(3.0f));

                if (_directory.Entries.Count == 0)
                    painter.AddImage(new Boundaries(Vector2.Zero, new Vector2(itemSize.X)), _folderOpenIcon, new Paint(new Color(1.0f, 0.5f)));
                else
                    painter.AddImage(new Boundaries(Vector2.Zero, new Vector2(itemSize.X)), _folderClosedIcon, new Paint(Color.White));

                if (_collection.ItemStyle.FontFamily != null && _collection.ItemStyle.FontFamily.IsReadyToUse)
                {
                    TextManager textManager = UIManager.Instance.TextManager;
                    TextBuilder textBuilder = new TextBuilder(itemSize.X, TextWrapMode.Ellipsis, TextAlignment.TopMiddle, false);
                    TextShapingData shapingData = textManager.ShapeText(_directory.Name, _collection.ItemStyle.FontSize, BuiltTextBuilder.Build(in textBuilder), _collection.ItemStyle.FontFamily.Value!.GetFontStyle(FontStyle.Normal, FontWeight.Normal));

                    painter.AddText(new Vector2(0.0f, float.Lerp(itemSize.X, itemSize.Y, 0.75f)), shapingData, new Vector2(itemSize.X, shapingData.TotalSize.Y), new Paint(_collection.ItemStyle.TextColor));
                }
            }
        }

        public override void HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            if (_directory != null)
            {
                if (inputEvent.EventType == UIInputEventType.MouseDown && inputEvent.Mouse.Button == MouseButton.Left)
                {
                    if (inputEvent.Mouse.Click == 1)
                        _collection.Select(_directory);
                    else if (inputEvent.Mouse.Click == 2)
                        _collection.ScopeTo(_directory);
                }
            }
        }

        public FilesystemDirectory? Directory { get => _directory; set => _directory = value; }
    }
}
