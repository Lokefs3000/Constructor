using CommunityToolkit.HighPerformance;
using Editor.UI.Menu;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common.Streams;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Assets.Loaders
{
    internal class ContextMenuAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new ContextMenuAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not ContextMenuAssetData contextMenuData)
                throw new ArgumentException(nameof(contextMenuData));

            return new ContextMenuAsset(contextMenuData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not ContextMenuAsset contextMenu)
                throw new ArgumentException(nameof(asset));
            if (assetData is not ContextMenuAssetData contextMenuData)
                throw new ArgumentException(nameof(assetData));

            try
            {
                using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom) ?? throw new AssetLoadException("Opened stream is null!");

                CtxMenuHeader header = stream.Read<CtxMenuHeader>();

                if (header.FileHeader != CtxMenuHeader.Header)
                    throw new AssetLoadException("Invalid header present");
                if (header.FileVersion != CtxMenuHeader.Version)
                    throw new AssetLoadException($"Incorrect file version {header.FileVersion} expected version {CtxMenuHeader.Version}");

                UIFontAsset fontAsset = AssetManager.LoadAsset<UIFontAsset>(header.FontAssetId);
                if (fontAsset.Status == ResourceStatus.Bad || fontAsset.Status == ResourceStatus.Error)
                    UIManager.Logger?.Warning("[{p}]: Loaded ui font currently has an invalid status {status}", sourcePath, fontAsset.Status);

                Span<char> nameBuffer = stackalloc char[byte.MaxValue];

                ContextMenuBase[] arr = new ContextMenuBase[header.ItemCount];
                DeserializeRecursive(arr, nameBuffer);

                contextMenuData.UpdateAssetData(contextMenu, [.. arr]);

                void DeserializeRecursive(ContextMenuBase[] childArray, Span<char> nameBuffer)
                {
                    for (int i = 0; i < childArray.Length; i++)
                    {
                        CtxMenuBase @base = stream.Read<CtxMenuBase>();

                        string id = string.Empty;
                        if (@base.IdLength > 0)
                        {
                            Span<char> temp = nameBuffer[..@base.IdLength];
                            stream.ReadExactly(MemoryMarshal.Cast<char, byte>(temp));

                            id = temp.ToString();
                        }

                        switch (@base.Type)
                        {
                            case CtxMenuType.Item:
                                {
                                    CtxMenuItem item = stream.Read<CtxMenuItem>();

                                    ContextMenuItem itemData = new ContextMenuItem()
                                    {
                                        Id = id,
                                        Font = fontAsset
                                    };

                                    if (item.TextLength > 0)
                                    {
                                        stream.ReadExactly(MemoryMarshal.Cast<char, byte>(nameBuffer[..item.TextLength]));
                                        itemData.Text = nameBuffer[..item.TextLength].ToString();
                                    }
                                    else
                                        itemData.Text = string.Empty;

                                    switch (item.ImageType)
                                    {
                                        case CtxMenuImageType.TextureAsset:
                                            {
                                                CtxMenuTextureAsset image = stream.Read<CtxMenuTextureAsset>();

                                                TextureAsset textureAsset = AssetManager.LoadAsset<TextureAsset>(image.Asset);
                                                if (textureAsset.Status == ResourceStatus.Bad || textureAsset.Status == ResourceStatus.Error)
                                                    UIManager.Logger?.Warning("[{p}]: Texture asset loaded for {item} image currently has an invalid status {status}", sourcePath, itemData.Text, textureAsset.Status);

                                                itemData.Image = textureAsset;
                                                break;
                                            }
                                        case CtxMenuImageType.Sprite:
                                            {
                                                CtxMenuSprite sprite = stream.Read<CtxMenuSprite>();

                                                TextureAtlasAsset textureAtlasAsset = AssetManager.LoadAsset<TextureAtlasAsset>(sprite.Asset);
                                                if (textureAtlasAsset.Status == ResourceStatus.Bad || textureAtlasAsset.Status == ResourceStatus.Error)
                                                    UIManager.Logger?.Warning("[{p}]: Texture atlas asset loaded for {item} image currently has an invalid status {status}", sourcePath, itemData.Text, textureAtlasAsset.Status);

                                                Span<char> temp = nameBuffer[..sprite.SpriteNameLength];
                                                if (!temp.IsEmpty)
                                                    stream.ReadExactly(MemoryMarshal.Cast<char, byte>(temp));

                                                Sprite? targetSprite = textureAtlasAsset.WaitIfNotLoaded().TryFindSpriteOrNull(temp.ToString());
                                                if (textureAtlasAsset.Status == ResourceStatus.Bad || textureAtlasAsset.Status == ResourceStatus.Error)
                                                    UIManager.Logger?.Warning("[{p}]: Sprite for {item} image was not found {spr}", sourcePath, itemData.Text, temp.ToString());

                                                itemData.Sprite = targetSprite;
                                                break;
                                            }
                                    }

                                    childArray[i] = itemData;

                                    if (item.ItemCount > 0)
                                    {
                                        ContextMenuBase[] arr = new ContextMenuBase[item.ItemCount];
                                        DeserializeRecursive(arr, nameBuffer);

                                        foreach (ContextMenuBase child in arr)
                                        {
                                            child.Parent = itemData;
                                        }
                                    }

                                    break;
                                }
                            case CtxMenuType.Separator:
                                {
                                    ContextMenuSeparator separatorItem = new ContextMenuSeparator()
                                    {
                                        Id = id
                                    };

                                    childArray[i] = separatorItem;
                                    break;
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                contextMenuData.UpdateAssetFailed(contextMenu);
                UIManager.Logger?.Error(ex, "Failed to load context menu: {name}", sourcePath);
            }
        }
    }

    public struct CtxMenuHeader
    {
        public int FileHeader;
        public int FileVersion;

        public AssetId FontAssetId;

        public byte ItemCount;

        public const int Header = 0x4d585443;
        public const int Version = 1;
    }

    public struct CtxMenuBase
    {
        public CtxMenuType Type;
        public byte IdLength;
    }

    public struct CtxMenuItem
    {
        public byte TextLength;
        public CtxMenuImageType ImageType;
        public byte ItemCount;
    }

    public struct CtxMenuTextureAsset
    {
        public AssetId Asset;
    }

    public struct CtxMenuSprite
    {
        public AssetId Asset;
        public byte SpriteNameLength;
    }

    public enum CtxMenuType : byte
    {
        Item = 0,
        Separator
    }

    public enum CtxMenuImageType : byte
    { 
        None = 0,
        TextureAsset,
        Sprite
    }
}
