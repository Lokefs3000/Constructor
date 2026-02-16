using Editor.UI.Modifiers;
using Editor.UI.Serialization.Values;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using TerraFX.Interop.Windows;

namespace Editor.UI.Serialization
{
    internal abstract class SerializationModifier : ISerializationType
    {
        private FrozenDictionary<string, ModifierPropertyData> _properties;
        private bool _useChildren;

        private Dictionary<string, ModifierPropertyData>? _temporaryProperties;

        internal SerializationModifier()
        {
            _useChildren = false;

            {
                _temporaryProperties = new Dictionary<string, ModifierPropertyData>();

                AddProperties();

                _properties = _temporaryProperties.ToFrozenDictionary();
                _temporaryProperties = null;
            }
        }

        protected void AddProperty<TMod, T>(string attributeName, string propertyName) where TMod : class, IUILayoutModifier
        {
            PropertyInfo? prop = typeof(TMod).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null)
                throw new NullReferenceException($"Failed to find property with name: {propertyName}");

            if (prop.PropertyType != typeof(T))
                throw new ArgumentException($"Specified type is not compatible with property: {propertyName}");

            MethodInfo? setMethodInfo = prop.GetSetMethod();
            if (setMethodInfo == null)
                throw new NullReferenceException($"Failed to find set method for property: {propertyName}");

            //FIXME: Potential bug if its a class and does not support setting null
            Action<TMod, T?> setAction = (Action<TMod, T?>)Delegate.CreateDelegate(typeof(Action<TMod, T?>), setMethodInfo);

            SetFieldValueDirect setValueDirect = (self, attrib) =>
            {
                if (ValueSerializerTable.Default.Deserialize(attrib.Value, out T? value))
                    setAction(Unsafe.As<TMod>(self), value);
                else
                    EdLog.Serialization.Error($"Failed to deserialize property: {attrib}");
            };

            _temporaryProperties!.Add(attributeName, new ModifierPropertyData(typeof(T), prop, setValueDirect));
        }

        protected virtual void UseNodeChildren() => _useChildren = true;

        protected abstract void AddProperties();

        protected virtual object? HandleNode(XmlNode node, IUILayoutModifier self, object? parent) => null;

        internal bool TryGetPropertyData(string attributeName, out ModifierPropertyData propertyData) => _properties.TryGetValue(attributeName, out propertyData);
        internal object? InvokeHandleNode(XmlNode node, IUILayoutModifier self, object? parent) => HandleNode(node, self, parent);

        public abstract Type Type { get; }
        public abstract string PrettyName { get; }

        internal bool UseChildren => _useChildren;
    }

    internal readonly record struct ModifierPropertyData(Type ValueType, PropertyInfo Field, SetFieldValueDirect FieldSetValue);

    internal delegate void SetFieldValueDirect(IUILayoutModifier self, XmlAttribute attribute);
}
