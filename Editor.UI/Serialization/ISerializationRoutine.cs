using CommunityToolkit.HighPerformance;
using Editor.UI;
using Editor.UI.Elements;
using Editor.UI.Reflection;
using Editor.UI.Serialization.Values;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using TerraFX.Interop.Windows;

namespace Editor.UI.Serialization
{
    public interface ISerializationRoutine
    {
        public UIElement? Deserialize(DeserializeContext context, UIElement? parentElement, XmlElement xmlElement, out bool skipChildren);
    }

    public readonly record struct DeserializeContext(XmlElement Xml, ReflectionManager ReflectionManager, ValueSerializerTable ValueSerializerTable)
    {
        public bool TryGetElementData(Type type, out CachedElementData elementData)
        {
            if (!ReflectionManager.ElementCache.TryGetElementData(type, out elementData))
            {
                UIManager.Logger?.Error("[{p}]: Failed to get element data for node", type);
                return false;
            }

            return true;
        }

        public bool TryGetElementData(string prettyName, out CachedElementData elementData)
        {
            if (!ReflectionManager.ElementCache.TryGetElementData(prettyName, out elementData))
            {
                UIManager.Logger?.Error("[{p}]: Failed to get element data for node", prettyName);
                return false;
            }

            return true;
        }

        public bool DeserializeProperty(XmlAttribute attrib, CachedElementData elementData, UIElement element)
        {
            if (attrib.Name == "Class")
            {
                foreach (var className in attrib.Value.Tokenize(' '))
                {
                    element.AddClass(className.ToString());
                }

                return true;
            }

            if (ReflectionManager.PropertyCache.TryFindProperty(elementData.TypeInfo, attrib.Name, out StyleProperty styleProperty))
            {
                object setterAction = ReflectionManager.MethodGenerator.GetSetterDelegate(styleProperty.Field);

                if (!ValueSerializerTable.DeserializeFieldSet(styleProperty.ObjectType, attrib.Value, setterAction, element, styleProperty.Field))
                {
                    UIManager.Logger?.Error("[{p}]: Failed to deserialize element property value: {v}", Xml.Name, attrib.Value);
                    return false;
                }
                else
                {
                    element.SetAsOverriden(styleProperty.Name);
                }
            }
            else
            {
                UIManager.Logger?.Error("[{p}]: Unknown attribute specified for element: {el}", Xml.Name, elementData.TypeInfo);
                return false;
            }

            return true;
        }
    }
}
