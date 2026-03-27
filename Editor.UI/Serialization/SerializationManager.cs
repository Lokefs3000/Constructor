using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Reflection;
using Editor.UI.Serialization.Values;
using Primary.Assets;
using Primary.Utility;
using System.Xml;

namespace Editor.UI.Serialization
{
    public sealed class SerializationManager
    {
        private ValueSerializerTable _valueSerializerTable;
        private RoutineManager _routineManager;

        internal SerializationManager()
        {
            _valueSerializerTable = new ValueSerializerTable();
            _routineManager = new RoutineManager();

            _valueSerializerTable.DiscoverAssembly(typeof(SerializationManager).Assembly);
        }

        internal void DeserializeLayout(UIWindow window, string layoutFile)
        {
            using Stream? stream = AssetFilesystem.OpenStream(layoutFile);
            if (stream == null)
            {
                UIManager.Logger?.Error("[{p}]: Failed to open stream for reading", layoutFile);
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            XmlElement? rootElement = doc.DocumentElement;

            if (rootElement == null)
            {
                UIManager.Logger?.Error("[{p}]: No root element found in layout", layoutFile);
                return;
            }

            if (rootElement.Name != "Layout")
            {
                UIManager.Logger?.Error("[{p}]: Root node name must be Layout", layoutFile);
                return;
            }

            UIManager manager = UIManager.Instance;

            PropertyCache propertyCache = manager.ReflectionManager.PropertyCache;
            MethodGenerator methodGenerator = manager.ReflectionManager.MethodGenerator;
            ElementCache elementCache = manager.ReflectionManager.ElementCache;
            ModifierCache modifierCache = manager.ReflectionManager.ModifierCache;

            //stage all changes
            List<StylesheetAsset> stylesheets = new List<StylesheetAsset>();
            UIElement? rootTreeElement = null;

            foreach (XmlNode node in rootElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement element = (XmlElement)node;

                    if (element.Name == "Stylesheet")
                    {
                        string? path = element.GetAttributeNode("Path")?.Value;
                        if (path == null)
                        {
                            UIManager.Logger?.Error("[{p}]: No path attribute specified on stylesheet element", layoutFile);
                            return;
                        }

                        if (!AssetFilesystem.Exists(path))
                        {
                            UIManager.Logger?.Error("[{p}]: No stylesheet exists at path: {p}", layoutFile, path);
                            return;
                        }

                        StylesheetAsset asset = AssetManager.LoadAsset<StylesheetAsset>(path);
                        if (!stylesheets.AddUnique(asset))
                        {
                            UIManager.Logger?.Error("[{p}]: Stylesheet: {p} has already been added to the layout previously", path);
                            return;
                        }
                    }
                    else if (element.Name == "Window")
                    {
                        if (rootTreeElement != null)
                        {
                            UIManager.Logger?.Error("[{p}]: Window content has already been defined previously", layoutFile);
                            return;
                        }

                        rootTreeElement = new UIElement();
                        foreach (XmlNode childNode in element.ChildNodes)
                        {
                            if (childNode.NodeType == XmlNodeType.Element)
                            {
                                if (!DefaultRoutine(rootTreeElement, (XmlElement)childNode))
                                    return;
                            }
                        }
                    }
                    else
                    {
                        UIManager.Logger?.Error("[{p}]: Unknown top-level element in layout: {n}", layoutFile, element.Name);
                        return;
                    }
                }
            }

            if (rootTreeElement != null)
            {
                foreach (StylesheetAsset stylesheet in stylesheets)
                {
                    window.StyleProvider.AddStylesheet(stylesheet);
                }

                while (rootTreeElement.Children.Count > 0)
                {
                    UIElement child = rootTreeElement.Children[0];

                    child.Parent = window.RootElement;
                    RecursiveApplyStyle(child);
                }

                void RecursiveApplyStyle(UIElement element)
                {
                    element.UpdateAllProperties();

                    foreach (UIElement child in element.Children)
                    {
                        RecursiveApplyStyle(child);
                    }
                }
            }

            bool DefaultRoutine(UIElement parent, XmlElement xml)
            {
                if (!elementCache.TryGetElementData(xml.Name, out CachedElementData elementData))
                {
                    if (!modifierCache.TryGetModifierData(xml.Name, out CachedModifierData modifierData))
                    {
                        UIManager.Logger?.Error("[{p}]: Failed to get element or modifier data for node", xml.Name);
                        return false;
                    }
                    else
                    {
                        return DefaultModifierRoutine(parent, xml, modifierData);
                    }
                }

                UIElement? element;
                if (elementData.CustomRoutine != null)
                {
                    if (_routineManager.TryGetRoutine(elementData.CustomRoutine, out ISerializationRoutine? routine))
                    {
                        DeserializeContext context = new DeserializeContext(xml, manager.ReflectionManager, _valueSerializerTable);
                        element = routine.Deserialize(context, parent, xml);

                        if (element == null)
                            return false;

                        goto DeserializeChildren;
                    }
                    else
                    {
                        UIManager.Logger?.Error("[{p}]: Failed to retrive custom serilization routine", xml.Name, elementData.CustomRoutine);
                    }
                }

                element = (UIElement)elementData.Constructor.Invoke(null);
                element.Parent = parent;

                foreach (XmlAttribute attrib in xml.Attributes)
                {
                    if (attrib.Name == "Class")
                    {
                        foreach (var className in attrib.Value.Tokenize(' '))
                        {
                            element.AddClass(className.ToString());
                        }

                        continue;
                    }

                    if (propertyCache.TryFindProperty(elementData.TypeInfo, attrib.Name, out StyleProperty styleProperty))
                    {
                        object setterAction = methodGenerator.GetSetterDelegate(styleProperty.Field);

                        if (!_valueSerializerTable.DeserializeFieldSet(styleProperty.ObjectType, attrib.Value, setterAction, element, styleProperty.Field))
                        {
                            UIManager.Logger?.Error("[{p}]: Failed to deserialize element property value: {v}", xml.Name, attrib.Value);
                            return false;
                        }
                        else
                        {
                            element.SetAsOverriden(attrib.Name);
                        }
                    }
                    else
                    {
                        UIManager.Logger?.Error("[{p}]: Unknown attribute {at} specified for element: {el}", xml.Name, attrib.Name, elementData.TypeInfo);
                        return false;
                    }
                }

            DeserializeChildren:
                foreach (XmlNode child in xml.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        if (!DefaultRoutine(element, (XmlElement)child))
                            return false;
                    }
                }

                return true;
            }
            bool DefaultModifierRoutine(UIElement parent, XmlElement xml, CachedModifierData modifierData)
            {
                IUILayoutModifier modifier = parent.AddLayoutModifier(modifierData.TypeInfo);

                foreach (XmlAttribute attrib in xml.Attributes)
                {
                    if (propertyCache.TryFindProperty(modifierData.TypeInfo, attrib.Name, out StyleProperty styleProperty))
                    {
                        object setterAction = methodGenerator.GetSetterDelegate(styleProperty.Field);

                        if (!_valueSerializerTable.DeserializeFieldSet(styleProperty.ObjectType, attrib.Value, setterAction, modifier, styleProperty.Field))
                        {
                            UIManager.Logger?.Error("[{p}]: Failed to deserialize modifier property value: {v}", xml.Name, attrib.Value);
                            return false;
                        }
                    }
                    else
                    {
                        UIManager.Logger?.Error("[{p}]: Unknown attribute specified for modifier: {el}", xml.Name, modifierData.TypeInfo);
                        return false;
                    }
                }

                return true;
            }
        }

        public ValueSerializerTable ValueSerializerTable => _valueSerializerTable;
    }
}
