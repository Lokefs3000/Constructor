using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Reflection;
using Editor.UI.Serialization.Values;
using Editor.UI.Styling;
using Primary.Assets;
using Primary.Rendering;
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

            SerializationContext ctx = new SerializationContext(elementCache, propertyCache, methodGenerator);

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
                                if (DefaultRoutine(ctx, rootTreeElement, (XmlElement)childNode) == null)
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
                    element.InvalidateAll();

                    if (element.LayoutModifiers != null)
                    {
                        foreach (IUILayoutModifier modifier in element.LayoutModifiers)
                        {
                            if (modifier is StyleBase styleBase)
                                styleBase.InvalidateAll();
                        }
                    }

                    foreach (UIElement child in element.Children)
                    {
                        RecursiveApplyStyle(child);
                    }
                }
            }
        }

        internal void DeserializeSnippet(LayoutSnippet snippet, string snippetFile)
        {
            using Stream? stream = AssetFilesystem.OpenStream(snippetFile);
            if (stream == null)
            {
                UIManager.Logger?.Error("[{p}]: Failed to open stream for reading", snippetFile);
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(stream);

            XmlElement? rootElement = doc.DocumentElement;

            if (rootElement == null)
            {
                UIManager.Logger?.Error("[{p}]: No root element found in snippet", snippetFile);
                return;
            }

            if (rootElement.Name != "Snippet")
            {
                UIManager.Logger?.Error("[{p}]: Root node name must be Snippet", snippetFile);
                return;
            }

            UIManager manager = UIManager.Instance;

            PropertyCache propertyCache = manager.ReflectionManager.PropertyCache;
            MethodGenerator methodGenerator = manager.ReflectionManager.MethodGenerator;
            ElementCache elementCache = manager.ReflectionManager.ElementCache;
            ModifierCache modifierCache = manager.ReflectionManager.ModifierCache;

            SerializationContext ctx = new SerializationContext(elementCache, propertyCache, methodGenerator);

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
                            UIManager.Logger?.Error("[{p}]: No path attribute specified on stylesheet element", snippetFile);
                            return;
                        }

                        if (!AssetFilesystem.Exists(path))
                        {
                            UIManager.Logger?.Error("[{p}]: No stylesheet exists at path: {p}", snippetFile, path);
                            return;
                        }

                        StylesheetAsset asset = AssetManager.LoadAsset<StylesheetAsset>(path);
                        if (!snippet.AddStylesheet(asset))
                        {
                            UIManager.Logger?.Error("[{p}]: Stylesheet: {p} has already been added to the snippet previously", path);
                            return;
                        }
                    }
                    else if (elementCache.Exists(node.Name))
                    {
                        if (snippet.RootElement != null)
                        {
                            UIManager.Logger?.Error("[{p}]: Window content has already been defined previously", snippetFile);
                            return;
                        }

                        object? root = DefaultRoutine(ctx, null, (XmlElement)node);
                        if (root == null || root is not UIElement)
                            return;

                        snippet.RootElement = (UIElement)root;
                    }
                    else
                    {
                        UIManager.Logger?.Error("[{p}]: Unknown top-level element in snippet: {n}", snippetFile, element.Name);
                        return;
                    }
                }
            }
        }

        private object? DefaultRoutine(SerializationContext ctx, UIElement? parent, XmlElement xml)
        {
            if (!ctx.ElementCache.TryGetElementData(xml.Name, out CachedElementData elementData))
            {
                UIManager.Logger?.Error("[{p}]: Failed to get element or modifier data for node", xml.Name);
                return null;
            }

            if (elementData.Constructor == null)
            {
                if (parent == null)
                    return null;
                return DefaultModifierRoutine(ctx, parent, xml, elementData);
            }

            UIElement? element;
            if (elementData.CustomRoutine != null)
            {
                if (_routineManager.TryGetRoutine(elementData.CustomRoutine, out ISerializationRoutine? routine))
                {
                    DeserializeContext context = new DeserializeContext(xml, UIManager.Instance.ReflectionManager, _valueSerializerTable);
                    element = routine.Deserialize(context, parent, xml, out bool skipChildren);

                    if (element == null)
                        return false;

                    if (skipChildren)
                        return true;

                    goto DeserializeChildren;
                }
                else
                {
                    UIManager.Logger?.Warning("[{p}]: Failed to retrive custom serilization routine", xml.Name, elementData.CustomRoutine);
                }
            }

            element = (UIElement)elementData.Constructor.Invoke(null);
            if (parent != null)
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

                if (ctx.PropertyCache.TryFindProperty(elementData.TypeInfo, attrib.Name, out StyleProperty styleProperty))
                {
                    if (styleProperty.Field == null)
                    {
                        if (!_valueSerializerTable.Deserialize(styleProperty.ObjectType, attrib.Value, out object? value))
                        {
                            UIManager.Logger?.Error("[{p}]: Failed to deserialize element property value: {v}", xml.Name, attrib.Value);
                            //return false;
                        }
                        else
                        {
                            styleProperty.Property.SetValue(element, value);
                            element.SetAsOverriden(attrib.Name);
                        }
                    }
                    else
                    {
                        object setterAction = ctx.MethodGenerator.GetSetterDelegate(styleProperty.Field);

                        if (!_valueSerializerTable.DeserializeFieldSet(styleProperty.ObjectType, attrib.Value, setterAction, element, styleProperty.Field))
                        {
                            UIManager.Logger?.Error("[{p}]: Failed to deserialize element property value: {v}", xml.Name, attrib.Value);
                            //return false;
                        }
                        else
                        {
                            element.SetAsOverriden(attrib.Name);
                        }
                    }
                }
                else
                {
                    UIManager.Logger?.Error("[{p}]: Unknown attribute {at} specified for element: {el}", xml.Name, attrib.Name, elementData.TypeInfo);
                    //return false;
                }
            }

        DeserializeChildren:
            foreach (XmlNode child in xml.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    if (DefaultRoutine(ctx, element, (XmlElement)child) == null)
                        return null;
                }
            }

            return element;
        }

        private bool DefaultModifierRoutine(SerializationContext ctx, UIElement parent, XmlElement xml, CachedElementData modifierData)
        {
            IUILayoutModifier modifier = parent.AddLayoutModifier(modifierData.TypeInfo);

            foreach (XmlAttribute attrib in xml.Attributes)
            {
                if (ctx.PropertyCache.TryFindProperty(modifierData.TypeInfo, attrib.Name, out StyleProperty styleProperty))
                {
                    if (styleProperty.Field == null)
                    {
                        if (!_valueSerializerTable.Deserialize(styleProperty.ObjectType, attrib.Value, out object? value))
                        {
                            UIManager.Logger?.Error("[{p}]: Failed to deserialize modifier property value: {v}", xml.Name, attrib.Value);
                            //return false;
                        }
                        else
                        {
                            styleProperty.Property.SetValue(modifier, value);
                        }
                    }
                    else
                    {
                        object setterAction = ctx.MethodGenerator.GetSetterDelegate(styleProperty.Field);

                        if (!_valueSerializerTable.DeserializeFieldSet(styleProperty.ObjectType, attrib.Value, setterAction, modifier, styleProperty.Field))
                        {
                            UIManager.Logger?.Error("[{p}]: Failed to deserialize modifier property value: {v}", xml.Name, attrib.Value);
                            //return false;
                        }
                    }
                }
                else
                {
                    UIManager.Logger?.Error("[{p}]: Unknown attribute {at} specified for modifier: {el}", xml.Name, attrib.Name, modifierData.TypeInfo);
                    //return false;
                }
            }

            return true;
        }

        public ValueSerializerTable ValueSerializerTable => _valueSerializerTable;

        private readonly record struct SerializationContext(ElementCache ElementCache, PropertyCache PropertyCache, MethodGenerator MethodGenerator);
    }
}
