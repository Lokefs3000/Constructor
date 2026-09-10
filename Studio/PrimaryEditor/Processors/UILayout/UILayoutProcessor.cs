using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using CommunityToolkit.HighPerformance;
using Primary.Assets.Types;
using Primary.Streams;
using PrimaryEditor.Assets;
using PrimaryEditor.Assets.Loaders;
using PrimaryEditor.Core;

namespace PrimaryEditor.Processors.UILayout
{
    public sealed class UILayoutProcessor
    {
        public static unsafe void Execute(Stream inputStream, Stream outputStream)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(inputStream);

            using PooledMemoryStream nodeStream = new PooledMemoryStream();

            Dictionary<string, uint> stringTable = new Dictionary<string, uint>();
            List<string> stringList = new List<string>();

            List<UILStylesheet> stylesheets = new List<UILStylesheet>();

            RecursiveParseXml(doc.DocumentElement!);

            outputStream.Write(new UILayoutHeader
            {
                FileHeader = UILayoutHeader.Header,
                FileVersion = UILayoutHeader.Version,

                StringTableSize = (uint)stringList.Count,
                StylesheetCount = (ushort)stylesheets.Count
            });

            foreach (string str in stringList)
            {
                if (str.Length > ushort.MaxValue)
                    throw new IndexOutOfRangeException($"String in table is longer than >{ushort.MaxValue}");

                outputStream.Write((ushort)str.Length);
                outputStream.Write(MemoryMarshal.Cast<char, byte>(str.AsSpan()));
            }

            foreach (UILStylesheet stylesheet in stylesheets)
            {
                outputStream.Write(stylesheet);
            }

            nodeStream.CopyTo(outputStream);

            void RecursiveParseXml(XmlElement node)
            {
                if (node.Name == "Stylesheet")
                {
                    if (node.HasChildNodes)
                        throw new Exception("Stylesheet cannot have child nodes");
                    if (node.Attributes.Count != 1)
                        throw new Exception("No path or id specified for stylesheet");

                    XmlAttribute attribute = node.Attributes[0];
                    if (attribute.Name == "Path")
                    {
                        if (!FilesystemManager.TryGetLocalPath(attribute.Value, out string? localPath))
                            throw new Exception($"Failed to get local path for stylesheet '{attribute.Value}'");
                        if (AssetPipeline.Instance == null || !AssetPipeline.Instance.AssetRegistry.TryLookupIdForPath(localPath, out FileId id))
                            throw new Exception($"Failed to get id for path '{localPath}'");

                        stylesheets.Add(new UILStylesheet { Id = id });
                    }
                    else if (attribute.Name == "Id")
                    {
                        if (!Guid.TryParse(attribute.Value, out Guid guid))
                            throw new Exception($"Failed to parse stylesheet id '{attribute.Value}'");

                        FileId assetId = new FileId(guid);
                        if (AssetPipeline.Instance == null || AssetPipeline.Instance.AssetRegistry.IsIdValid(assetId))
                            throw new Exception($"Invalid id specified for stylesheet '{assetId}'");

                        stylesheets.Add(new UILStylesheet { Id = assetId });
                    }

                    return;
                }

                int propertyCount = node.Attributes.Count;

                XmlAttribute? classAttribute = node.Attributes.GetNamedItem("Class") as XmlAttribute;
                if (classAttribute != null)
                    --propertyCount;

                int childCount = 0;
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child is XmlElement element)
                    {
                        if (child.Name != "Stylesheet")
                            ++childCount;
                    }
                }

                nodeStream.Write(new UILNode
                {
                    NameId = GetStringId(node.Name),

                    ClassCount = (ushort)(classAttribute == null || classAttribute.Value.Length == 0 ? 0 : classAttribute.Value.Count(' ') + 1),
                    PropertyCount = (ushort)propertyCount,
                    ChildCount = (ushort)childCount
                });

                if (classAttribute != null)
                {
                    foreach (ReadOnlySpan<char> className in classAttribute.Value.Tokenize(' '))
                    {
                        nodeStream.Write((ushort)GetStringId(className.ToString()));
                    }
                }

                foreach (XmlAttribute attribute in node.Attributes)
                {
                    if (attribute == classAttribute)
                        continue;

                    nodeStream.Write(new UILProperty
                    {
                        NameId = GetStringId(attribute.Name),
                        ValueId = GetStringId(attribute.Value)
                    });
                }

                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child is XmlElement element)
                        RecursiveParseXml(element);
                }
            }

            uint GetStringId(string str)
            {
                ref uint value = ref CollectionsMarshal.GetValueRefOrAddDefault(stringTable, str, out bool exists);
                if (!exists)
                {
                    value = (uint)stringList.Count;
                    stringList.Add(str);
                }

                return value;
            }
        }
    }
}
