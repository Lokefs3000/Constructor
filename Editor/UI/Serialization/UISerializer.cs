using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Editor.UI.Serialization
{
    public static class UISerializer
    {
        public static bool TryDeserialize(string xml, UIElement parentElement)
        {
            SerializationTable table = SerializationTable.Default;

            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);

            XmlNode rootNode = document.DocumentElement!;
            return RecursiveDecode(rootNode, parentElement);

            bool RecursiveDecode(XmlNode node, UIElement parent)
            {
                UIElement? element;
                if (node.Name != "Root")
                {
                    element = table.DeserializeNode(node, parent);
                    if (element == null)
                    {
                        EdLog.Serialization.Error("Error occured during deserialization of node");
                        return false;
                    }
                }
                else
                    element = parent;

                bool ret = true;
                if (node.ChildNodes.Count > 0)
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        if (child.LocalName.StartsWith("mod"))
                        {
                            table.DeserializeModifier(child, element);
                            continue;
                        }

                        ret = RecursiveDecode(child, element) && ret;
                    }
                }

                return ret;
            }
        }
    }
}
