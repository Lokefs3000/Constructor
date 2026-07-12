using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Utility;

namespace PrimaryEditor.Assets.Loaders
{
    internal class UILayoutAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new UILayoutData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            return new UILayoutAsset((UILayoutData)assetData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, BundleReader? bundleToReadFrom)
        {
            UILayoutAsset layout = (UILayoutAsset)asset;
            UILayoutData layoutData = (UILayoutData)assetData;

            using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom)
                ?? throw new AssetLoadException("Failed to open stream for reading");

            if (!stream.TryRead(out UILayoutHeader headerData))
                throw new AssetLoadException("Issue occured reading header");

            if (headerData.FileHeader != UILayoutHeader.Header)
                throw new AssetLoadException($"Invalid header present in file '{headerData.FileHeader:x8}'");
            if (headerData.FileVersion != UILayoutHeader.Version)
                throw new AssetLoadException($"Invalid version present in file '{headerData.FileVersion}'");

            string[] stringTable = new string[headerData.StringTableSize];
            {
                using RentedArray<char> stringChars = RentedArray<char>.Rent(ushort.MaxValue);
                for (int i = 0; i < headerData.StringTableSize; ++i)
                {
                    ushort textLength = stream.Read<ushort>();
                    stream.ReadExactly(MemoryMarshal.Cast<char, byte>(stringChars.Span[..textLength]));

                    stringTable[i] = stringChars.Span[..textLength].ToString();
                }
            }

            StylesheetAsset[] stylesheets = new StylesheetAsset[headerData.StylesheetCount];
            for (int i = 0; i < stylesheets.Length; ++i)
            {
                UILStylesheet stylesheetData = stream.Read<UILStylesheet>();
                stylesheets[i] = AssetManager.LoadAsset<StylesheetAsset>(stylesheetData.Id);
            }

            UILayoutNode rootNode = RecursiveReadNodes();
            layoutData.UpdateAssetData(layout, rootNode, [.. stylesheets]);

            UILayoutNode RecursiveReadNodes()
            {
                UILNode node = stream.Read<UILNode>();

                string[] classList = node.ClassCount == 0 ? [] : new string[node.ClassCount];
                UILayoutPropertyValue[] propertyValues = node.PropertyCount == 0 ? [] : new UILayoutPropertyValue[node.PropertyCount];
                UILayoutNode[] layoutNodes = node.ChildCount == 0 ? [] : new UILayoutNode[node.ChildCount];

                for (int i = 0; i < node.ClassCount; i++)
                {
                    classList[i] = stringTable[stream.Read<ushort>()];
                }

                for (int i = 0; i < node.PropertyCount; ++i)
                {
                    UILProperty property = stream.Read<UILProperty>();
                    propertyValues[i] = new UILayoutPropertyValue(stringTable[property.NameId], stringTable[property.ValueId]);
                }

                for (int i = 0; i < node.ChildCount; ++i)
                {
                    layoutNodes[i] = RecursiveReadNodes();
                }

                return new UILayoutNode(stringTable[node.NameId], [.. classList], [.. propertyValues], [.. layoutNodes], default);
            }
        }
    }

    public struct UILayoutHeader
    {
        public uint FileHeader;
        public uint FileVersion;

        public uint StringTableSize;
        public ushort StylesheetCount;

        public const uint Header = 0x4f4c4955;
        public const uint Version = 1;
    }

    public struct UILStylesheet
    {
        public AssetId Id;
    }

    public struct UILNode
    {
        public uint NameId;

        public ushort ClassCount;
        public ushort PropertyCount;
        public ushort ChildCount;
    }

    public struct UILProperty
    {
        public uint NameId;
        public uint ValueId;
    }
}
