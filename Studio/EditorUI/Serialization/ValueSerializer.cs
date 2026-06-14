using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EditorUI.Serialization.Value;
using EditorUI.Utility;

namespace EditorUI.Serialization
{
    public sealed class ValueSerializer
    {
        private Dictionary<Type, ValueConverter> _converters;

        internal ValueSerializer()
        {
            _converters = new Dictionary<Type, ValueConverter>();
        }

        internal void LoadConverterFromType(Type type, object attributeData)
        {
            if (!type.IsClass || type.BaseType == null || !type.BaseType.IsGenericType)
                return;

            Type genericBase = type.BaseType.GetGenericTypeDefinition();
            if (genericBase != typeof(ValueConverter<>))
                return;

            Type targetType = type.BaseType.GenericTypeArguments[0];
            if (_converters.ContainsKey(targetType))
                return;

            ValueConverter obj;
            try
            {
                obj = (ValueConverter)Activator.CreateInstance(type)!;
            }
            catch (Exception)
            {
                return;
            }

            _converters.Add(targetType, obj);
        }

        public bool TryDeserialize<T>(string source, out T? value, out Exception? exception)
        {
            try
            {
                Type type = typeof(T);
                if (type.IsEnum)
                {
                    exception = null;
                    value = EnumHelper<T>.Parse(source, false);

                    return true;
                }
                else
                {
                    if (_converters.TryGetValue(type, out ValueConverter? converter))
                    {
                        value = Unsafe.As<ValueConverter<T>>(converter).TryDeserialize(source);
                        exception = null;

                        return true;
                    }
                    else
                    {
                        throw new Exception($"No value converter specified for type {type}");
                    }
                }
            }
            catch (Exception ex)
            {
                value = default;
                exception = ex;

                return false;
            }
        }

        public bool TryDeserialize(Type type, string source, out object? value, out Exception? exception)
        {
            try
            {
                if (type.IsEnum)
                {
                    exception = null;
                    value = Enum.Parse(type, source, false);

                    return true;
                }
                else
                {
                    if (_converters.TryGetValue(type, out ValueConverter? converter))
                    {
                        value = converter.TryDeserializeBoxed(source);
                        exception = null;

                        return true;
                    }
                    else
                    {
                        throw new Exception($"No value converter specified for type {type}");
                    }
                }
            }
            catch (Exception ex)
            {
                value = default;
                exception = ex;

                return false;
            }
        }
    }
}
