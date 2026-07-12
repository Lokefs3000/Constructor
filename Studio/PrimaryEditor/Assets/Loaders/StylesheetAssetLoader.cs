using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Styling;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Common.Streams;
using Primary.Rendering;
using Primary.Utility;
using TerraFX.Interop.Windows;

namespace PrimaryEditor.Assets.Loaders
{
    public sealed class StylesheetAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new StylesheetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            return new StylesheetAsset((StylesheetData)assetData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, string localPath, BundleReader? bundleToReadFrom)
        {
            StylesheetAsset stylesheet = (StylesheetAsset)asset;
            StylesheetData stylesheetData = (StylesheetData)assetData;

            using Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom)
               ?? throw new AssetLoadException("Failed to open stream for reading");

            if (!stream.TryRead(out StylesheetHeader headerData))
                throw new AssetLoadException("Issue occured reading header");

            if (headerData.FileHeader != StylesheetHeader.Header)
                throw new AssetLoadException($"Invalid header present in file '{headerData.FileHeader:x8}'");
            if (headerData.FileVersion != StylesheetHeader.Version)
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

            Stylesheet stylesheetObj = new Stylesheet(sourcePath);

            for (int i = 0; i < headerData.ClassCount; ++i)
            {
                SSTClass classData = stream.Read<SSTClass>();

                string stylesheetName = stringTable[classData.NameSId];
                StylesheetClass classObj = stylesheetObj.CreateClass(stylesheetName[0] == '.' ? ClassType.Named : ClassType.Typed, stylesheetName);

                ReadPropertiesFor(classData.PropertyGroupCount, classObj);

                for (int j = 0; j < classData.ChildCount; ++j)
                {
                    SSTPseudoClass pseudoClass = stream.Read<SSTPseudoClass>();

                    PseduoClassType classType = pseudoClass.Type switch
                    {
                        SSTPseudoClassType.FirstChild => PseduoClassType.FirstChild,
                        SSTPseudoClassType.LastChild => PseduoClassType.LastChild,
                        SSTPseudoClassType.OnlyChild => PseduoClassType.OnlyChild,
                        SSTPseudoClassType.FirstOfType => PseduoClassType.FirstOfType,
                        SSTPseudoClassType.LastOfType => PseduoClassType.LastOfType,
                        SSTPseudoClassType.OnlyOfType => PseduoClassType.OnlyOfType,
                        SSTPseudoClassType.AllChildren => PseduoClassType.AllChildren,
                        _ => throw new NotImplementedException(),
                    };
                    string? className = pseudoClass.NameSId == uint.MaxValue ? null : stringTable[pseudoClass.NameSId];

                    StylesheetClass pseudoClassObj = classObj.CreateSubClass(classType, className ?? string.Empty);

                    ReadPropertiesFor(pseudoClass.PropertyGroupCount, pseudoClassObj);
                }
            }

            stylesheetData.UpdateAssetData(stylesheet, stylesheetObj);

            void ReadPropertiesFor(ushort propertyGroupCount, StylesheetClass classObj)
            {
                for (int j = 0; j < propertyGroupCount; ++j)
                {
                    SSTPropertyGroup propertyGroup = stream.Read<SSTPropertyGroup>();

                    ushort triggerMask = 0;
                    bool errorWhenFindingTriggers = false;

                    for (int k = 0; k < propertyGroup.TriggerCount; ++k)
                    {
                        SSTTrigger trigger = stream.Read<SSTTrigger>();

                        if (TriggerValues.Triggers.TryGetValue(stringTable[trigger.NameSId], out ushort triggerMaskForThis))
                            triggerMask |= triggerMaskForThis;
                        else
                        {
                            EdLog.Assets.Error("[{file}]: Failed to find trigger with name '{n}'", localPath, stringTable[k]);
                            errorWhenFindingTriggers = true;
                        }
                    }

                    if (errorWhenFindingTriggers)
                    {
                        stream.Seek(Unsafe.SizeOf<SSTProperty>() * propertyGroup.PropertyCount, SeekOrigin.Current);
                    }
                    else
                    {
                        for (int k = 0; k < propertyGroup.PropertyCount; ++k)
                        {
                            SSTProperty property = stream.Read<SSTProperty>();
                            classObj.SetStyleValue(new StyleKey(stringTable[property.NameSId]), stringTable[property.ValueSId], propertyGroup.TriggerCount == 0 ? -1 : triggerMask);
                        }
                    }
                }
            }
        }
    }

    public struct StylesheetHeader
    {
        public uint FileHeader;
        public uint FileVersion;

        public uint StringTableSize;
        public uint ClassCount;

        public const uint Header = 0x4c595453;
        public const uint Version = 1;
    }

    public struct SSTClass
    {
        public uint NameSId;

        public ushort PropertyGroupCount;
        public ushort ChildCount;
    }

    public struct SSTPropertyGroup
    {
        public byte TriggerCount;
        public ushort PropertyCount;
    }

    public struct SSTTrigger
    {
        public uint NameSId;
    }

    public struct SSTProperty
    {
        public uint NameSId;
        public uint ValueSId;
    }

    public struct SSTPseudoClass
    {
        public SSTPseudoClassType Type;
        public uint NameSId;

        public ushort PropertyGroupCount;
    }

    public enum SSTPseudoClassType : byte
    {
        FirstChild = 0,
        LastChild,
        OnlyChild,

        FirstOfType,
        LastOfType,
        OnlyOfType,

        AllChildren,
    }
}
