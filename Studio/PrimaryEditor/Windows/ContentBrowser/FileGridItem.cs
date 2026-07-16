using System;
using System.Collections.Frozen;
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
using PrimaryEditor.Core;

namespace PrimaryEditor.Windows.ContentBrowser
{
    internal sealed class FileGridItem : GridViewItem
    {
        private readonly EntryGridCollection _collection;

        private readonly FrozenDictionary<string, Sprite?> _sprites;
        private readonly Sprite? _fileDefault;
        private readonly Sprite? _errorSprite;

        private readonly FrozenDictionary<string, Sprite?>.AlternateLookup<ReadOnlySpan<char>> _spritesAlt;

        private FilesystemFile? _file;

        internal FileGridItem(EntryGridCollection collection)
        {
            _collection = collection;

            TextureAtlasAsset textureAtlas = AssetManager.LoadAsset<TextureAtlasAsset>("Editor/UI/Icons/DefaultFileIcons.atlas").WaitIfNotLoaded();
            _sprites = new Dictionary<string, Sprite?>
            {
                { ".txt", textureAtlas.TryFindSpriteOrNull("FileText") },
                { ".log", textureAtlas.TryFindSpriteOrNull("FileText") },
                { ".json", textureAtlas.TryFindSpriteOrNull("FileText") },
                { ".toml", textureAtlas.TryFindSpriteOrNull("FileText") },
                { ".png", textureAtlas.TryFindSpriteOrNull("FileTexture") },
                { ".jpg", textureAtlas.TryFindSpriteOrNull("FileTexture") },
                { ".jpeg", textureAtlas.TryFindSpriteOrNull("FileTexture") },
                { ".shader", textureAtlas.TryFindSpriteOrNull("FileShader") },
                { ".compute", textureAtlas.TryFindSpriteOrNull("FileComputeShader") },
                { ".atlas", textureAtlas.TryFindSpriteOrNull("FileTextureAtlas") },
                { ".obj", textureAtlas.TryFindSpriteOrNull("Model") },
                { ".fbx", textureAtlas.TryFindSpriteOrNull("Model") },
                { ".mat", textureAtlas.TryFindSpriteOrNull("Material") },
                { ".scene", textureAtlas.TryFindSpriteOrNull("Scene") },
            }.ToFrozenDictionary();
            _fileDefault = textureAtlas.TryFindSpriteOrNull("FileDefault");
            _errorSprite = textureAtlas.TryFindSpriteOrNull("Error");

            _spritesAlt = _sprites.GetAlternateLookup<ReadOnlySpan<char>>();

            _file = null;
        }

        public override void OnPaint(GridView gridView, ref readonly PainterContext painter, Vector2 itemSize)
        {
            if (_file != null && _collection.ItemStyle != null)
            {
                if (_collection.IsSelected(_file))
                    painter.AddRectangle(new Boundaries(Vector2.Zero, itemSize), new Paint(_collection.ItemStyle.SelectedColor), new Vector4(3.0f));

                int extensionIndex = _file.LocalPath.LastIndexOf('.');
                ReadOnlySpan<char> extension = extensionIndex == -1 ? ReadOnlySpan<char>.Empty : _file.LocalPath.AsSpan()[extensionIndex..];

                if (extension.IsEmpty || !_spritesAlt.TryGetValue(extension, out Sprite? sprite))
                    painter.AddImage(new Boundaries(Vector2.Zero, new Vector2(itemSize.X)), _fileDefault, new Paint(Color.White));
                else
                    painter.AddImage(new Boundaries(Vector2.Zero, new Vector2(itemSize.X)), sprite, new Paint(Color.White));

                if (_collection.ItemStyle.FontFamily != null && _collection.ItemStyle.FontFamily.IsReadyToUse)
                {
                    TextManager textManager = UIManager.Instance.TextManager;
                    TextBuilder textBuilder = new TextBuilder(itemSize.X, TextWrapMode.Ellipsis, TextAlignment.TopMiddle, false);
                    TextShapingData shapingData = textManager.ShapeText(_file.Name, _collection.ItemStyle.FontSize, BuiltTextBuilder.Build(in textBuilder), _collection.ItemStyle.FontFamily.Value!.GetFontStyle(FontStyle.Normal, FontWeight.Normal));

                    painter.AddText(new Vector2(0.0f, float.Lerp(itemSize.X, itemSize.Y, 0.75f)), shapingData, new Vector2(itemSize.X, shapingData.TotalSize.Y), new Paint(_collection.ItemStyle.TextColor));
                }
            }
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            if (_file != null)
            {
                if (inputEvent.EventType == UIInputEventType.MouseDown && inputEvent.Mouse.Button == MouseButton.Left)
                {
                    if (inputEvent.Mouse.Click == 1)
                        _collection.Select(_file);
                    else if (inputEvent.Mouse.Click == 2)
                        _collection.ScopeTo(_file);

                    return true;
                }
            }

            return false;
        }

        public FilesystemFile? File { get => _file; set => _file = value; }
    }
}
