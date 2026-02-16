using Editor.UI.Elements;
using Editor.UI.Serialization.Values;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Editor.UI.Serialization
{
    internal abstract class SerializationGroup : ISerializationType
    {
        private FrozenDictionary<string, SerilizationAttribute> _attributes;
        private Dictionary<string, SerilizationAttribute>? _tempAttributes;

        internal SerializationGroup()
        {
            _attributes = FrozenDictionary<string, SerilizationAttribute>.Empty;

            _tempAttributes = new Dictionary<string, SerilizationAttribute>();
            RegisterAttributes();

            _attributes = _tempAttributes.ToFrozenDictionary();
            _tempAttributes = null;
        }

        protected void AddAttribute<TElem, T>(string name, Action<TElem, T?> setter) where TElem : UIElement
        {
            Action<UIElement, XmlAttribute> proxySetter;
            if (false && typeof(T).IsEnum)
            {
                proxySetter = (x, y) =>
                {
                    
                };
            }
            else
            {
                proxySetter = (x, y) =>
                {
                    if (ValueSerializerTable.Default.Deserialize(y.Value, out T? value))
                        setter(Unsafe.As<TElem>(x), value);
                    else
                        EdLog.Serialization.Error("Failed to deserialize attribute: {n} ({vt}) on element: {et}", name, typeof(T).Name, typeof(TElem).Name);
                };
            }

            _tempAttributes!.Add(name, new SerilizationAttribute(typeof(T), proxySetter));
        }

        public abstract UIElement CreateInstance(UIElement parent);

        protected abstract void RegisterAttributes();

        public abstract Type Type { get; }
        public abstract string PrettyName { get; }

        internal IReadOnlyDictionary<string, SerilizationAttribute> Attributes => _attributes;
    }

    internal readonly record struct SerilizationAttribute(Type ValueType, Action<UIElement, XmlAttribute> Setter);
}
