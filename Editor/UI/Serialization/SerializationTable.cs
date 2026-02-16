using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Serialization.Groups;
using Editor.UI.Serialization.Utility;
using Editor.UI.Serialization.Values;
using Editor.UI.Serialization.Values.Structs;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;

namespace Editor.UI.Serialization
{
    internal sealed class SerializationTable
    {
        private SerializationDictionary<SerializationGroup> _serializers;
        private SerializationDictionary<SerializationModifier> _modifiers;

        private SerializationTable()
        {
            _serializers = new SerializationDictionary<SerializationGroup>();
            _modifiers = new SerializationDictionary<SerializationModifier>();

            TryLoadAllDefaultSerializers();
        }

        private void TryLoadAllDefaultSerializers()
        {
            Assembly assembly = typeof(SerializationTable).Assembly;
            IEnumerable<Type> types = assembly.GetTypes().Where((x) =>
            {
                if (!x.IsAssignableTo(typeof(ISerializationType)))
                    return false;

                if (x.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) == null)
                    return false;

                return true;
            });

            foreach (Type type in types)
            {
                if (type.BaseType == typeof(SerializationGroup))
                {
                    SerializationGroup group = (SerializationGroup)Activator.CreateInstance(type)!;
                    Type groupType = group.Type;

                    if (_serializers.TryAdd(group))
                    {

                    }
                    else
                        EdLog.Serialization.Warning("Duplicate serilization group type: {t}", groupType);
                }
                else if (type.BaseType == typeof(SerializationModifier))
                {
                    SerializationModifier modifier = (SerializationModifier)Activator.CreateInstance(type)!;
                    Type modifierType = modifier.Type;

                    if (_modifiers.TryAdd(modifier))
                    {

                    }
                    else
                        EdLog.Serialization.Warning("Duplicate serilization modifier type: {t}", modifierType);
                }
            }
        }

        internal UIElement? DeserializeNode(XmlNode node, UIElement parent)
        {
            if (!_serializers.TryGetTyping(node.LocalName, out Type? type))
            {
                EdLog.Serialization.Error("Failed to find type for node: {n}", node.LocalName);
                return null;
            }

            if (!_serializers.TryGetValue(type, out SerializationGroup? group))
            {
                EdLog.Serialization.Error("Failed to find serializer for element: {t}", type.Name);
                return null;
            }

            UIElement element = group.CreateInstance(parent);
            Debug.Assert(element.GetType() == type);

            element.Parent = parent;

            foreach (XmlAttribute attrib in node.Attributes!)
            {
                bool didSetValue = false;

                Type? @base = type;
                do
                {
                    SerializationGroup? baseGroup = null;
                    if (@base == type)
                        baseGroup = group;
                    else
                    {
                        if (!_serializers.TryGetValue(@base, out baseGroup))
                        {
                            EdLog.Serialization.Error("Failed to find serializer for base element: {t}", @base.Name);
                            continue;
                        }
                    }

                    if (baseGroup.Attributes.TryGetValue(attrib.Name, out SerilizationAttribute data))
                    {
                        data.Setter(element, attrib);
                        didSetValue = true;
                    }
                } while ((@base = @base.BaseType) != null && @base != typeof(object));

                if (!didSetValue)
                {
                    EdLog.Serialization.Error("Failed to deserialize attribute: {n} on element: {e}", attrib.Name, type.Name);
                }
            }

            return element;
        }

        internal void DeserializeModifier(XmlNode node, UIElement owner)
        {
            if (!_modifiers.TryGetTyping(node.LocalName.Substring(3), out Type? type))
            {
                EdLog.Serialization.Error("Failed to find type for node: {n}", node.LocalName);
                return;
            }

            if (!_modifiers.TryGetValue(type, out SerializationModifier? modifier))
            {
                EdLog.Serialization.Error("Failed to find serializer for modifier: {t}", type.Name);
                return;
            }

            IUILayoutModifier value = owner.AddLayoutModifier(type);

            foreach (XmlAttribute attrib in node.Attributes!)
            {
                if (modifier.TryGetPropertyData(attrib.Name, out ModifierPropertyData propertyData))
                {
                    propertyData.FieldSetValue(value, attrib);
                }
            }

            if (modifier.UseChildren)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    RecursiveChild(child, null);
                }

                void RecursiveChild(XmlNode currentNode, object? parent)
                {
                    object? result = modifier.InvokeHandleNode(node, value, parent);
                    foreach (XmlNode child in currentNode.ChildNodes)
                    {
                        RecursiveChild(child, result);
                    }
                }
            }
        }

        internal static readonly SerializationTable Default = new SerializationTable();
    }
}
