using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Reflection;
using Editor.UI.Serialization.Values;
using Editor.UI.Styling;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common.Streams;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Editor.UI.Assets.Loaders
{
    internal sealed class StylesheetAssetLoader : IAssetLoader
    {
        public IInternalAssetData FactoryCreateNull(AssetId id)
        {
            return new StylesheetAssetData(id);
        }

        public IAssetDefinition FactoryCreateDef(IInternalAssetData assetData)
        {
            if (assetData is not StylesheetAssetData stylesheetData)
                throw new ArgumentException(nameof(stylesheetData));

            return new StylesheetAsset(stylesheetData);
        }

        public void FactoryLoad(IAssetDefinition asset, IInternalAssetData assetData, string sourcePath, BundleReader? bundleToReadFrom)
        {
            if (asset is not StylesheetAsset stylesheet)
                throw new ArgumentException(nameof(asset));
            if (assetData is not StylesheetAssetData stylesheetData)
                throw new ArgumentException(nameof(assetData));

            ILogger logger = UIManager.Logger!;

            try
            {
                Stream? stream = AssetFilesystem.OpenStream(sourcePath, bundleToReadFrom);
                if (stream == null)
                {
                    stylesheetData.UpdateAssetFailed(stylesheet);
                    return;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(stream);

                if (doc.DocumentElement == null)
                {
                    stylesheetData.UpdateAssetFailed(stylesheet);
                    return;
                }

                Dictionary<string, StylesheetClass> classes = new Dictionary<string, StylesheetClass>();
                HashSet<string> activeStates = new HashSet<string>();

                ElementCache elementCache = UIManager.Instance.ReflectionManager.ElementCache;

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        XmlElement element = (XmlElement)node;
                        if (element.Name == "Class")
                        {
                            string? targetType = element.GetAttributeNode("Type")?.Value;
                            if (targetType == null)
                            {
                                logger.Error("[{p}]: No target defined for class in stylesheet", sourcePath);
                                continue;
                            }

                            string className = element.GetAttributeNode("Name")?.Value ?? targetType;
                            if (classes.ContainsKey(className))
                            {
                                logger.Error("[{p}]: Class with name: {n} has already been defined in previously in the stylesheet", sourcePath, className);
                                continue;
                            }

                            if (!elementCache.TryGetElementData(targetType, out CachedElementData elementData))
                            {
                                logger.Error("[{p}]: Class target: {n} has either not been cached yet or does not exist", sourcePath, targetType);
                                continue;
                            }

                            StylesheetClass stylesheetClass = new StylesheetClass(false);
                            
                            classes.Add(className, stylesheetClass);
                            activeStates.Clear();

                            if (element.HasChildNodes)
                            {
                                bool isParsingState = false;

                                foreach (XmlNode valueNode in element.ChildNodes)
                                {
                                    if (valueNode.NodeType == XmlNodeType.Element)
                                    {
                                        XmlElement valueElement = (XmlElement)valueNode;

                                        if (!isParsingState && valueNode.Name != "State")
                                        {
                                            ParseClassValues(element, stylesheetClass, elementData.TypeInfo, className);
                                            break;
                                        }
                                        else
                                            isParsingState = true;

                                        if (valueNode.Name == "State")
                                        {
                                            string stateName = valueElement.GetAttribute("Name");
                                            if (!activeStates.Add(stateName))
                                            {
                                                logger.Error("[{p}]: Stylesheet class already has a previous decleration with state name: {n}", sourcePath, stateName);
                                                continue;
                                            }

                                            ParseClassValues(valueElement, stylesheetClass, elementData.TypeInfo, $"{className}:{stateName}", stateName);
                                        }
                                        else
                                        {
                                            logger.Error("[{p}]: Expected state node within class but instead found: {n}", sourcePath, valueElement.Name);
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            logger.Error("[{p}]: Undefined stylesheet element name found as top level node: {n}", sourcePath, element.Name);
                            continue;
                        }
                    }
                }

                void ParseClassValues(XmlNode parentNode, StylesheetClass stylesheetClass, Type elementType, string fullPath, string stateName = "Normal")
                {
                    ValueSerializerTable valueSerializerTable = UIManager.Instance.SerializationManager.ValueSerializerTable;
                    PropertyCache propertyCache = UIManager.Instance.ReflectionManager.PropertyCache;

                    foreach (XmlNode node in parentNode.ChildNodes)
                    {
                        if (node.NodeType == XmlNodeType.Element)
                        {
                            XmlElement element = (XmlElement)node;

                            string? value = element.GetAttributeNode("Value")?.Value;
                            if (value == null)
                            {
                                logger.Error("[{p}]: Expected value for stylesheet class property: {n}", sourcePath, element.Name);
                                continue;
                            }

                            if (stylesheetClass.HasProperty(element.Name, stateName))
                            {
                                logger.Error("[{p}]: Stylesheet class already has a property defined with name: {n}", sourcePath, element.Name);
                                continue;
                            }

                            if (propertyCache.TryFindProperty(elementType, node.Name, out StyleProperty styleProperty) && styleProperty.Type == StylePropertyType.Styleable)
                            {
                                if (valueSerializerTable.Deserialize(styleProperty.ObjectType, value, out object? result))
                                    stylesheetClass.SetProperty(result, node.Name, stateName);
                                else
                                {
                                    logger.Error("[{p}]: Failed to deserialize style property value: {n} on property: {p}", sourcePath, value, styleProperty.Name);
                                    continue;
                                }
                            }
                            else
                            {
                                logger.Error("[{p}]: Element {t} does not have a styleable property with name: {n}", sourcePath, elementType, element.Name);
                                continue;
                            }
                        }
                    }
                }

                stylesheetData.UpdateAssetData(stylesheet, classes);
            }
#if DEBUG
            finally
            {

            }
#else
            catch (Exception ex)
            {
                stylesheetData.UpdateAssetFailed(stylesheet);
                UIManager.Logger?.Error(ex, "Failed to load stylesheet: {name}", sourcePath);
            }
#endif
        }
    }
}
