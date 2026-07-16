using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CommunityToolkit.HighPerformance;

namespace PrimaryEditor.Inspector.Views
{
    internal readonly ref struct ViewTypeResolver
    {
        private readonly Type _sourceType;
        private readonly Dictionary<string, Type> _resolvedTypes;

        internal ViewTypeResolver(Type sourceType)
        {
            _sourceType = sourceType;
            _resolvedTypes = new Dictionary<string, Type>();
        }

        internal ViewSource ResolveSource(string name)
        {
            var tokenizer = name.Tokenize('.');
            tokenizer.MoveNext();

            Type currentType = _sourceType;
            while (true)
            {
                string currentValue = tokenizer.Current.ToString();
                if (tokenizer.MoveNext())
                {
                    if (_resolvedTypes.TryGetValue(currentValue, out Type? type))
                    {
                        currentType = type;
                    }
                    else
                    {
                        type =
                            currentType.GetField(currentValue)?.FieldType ??
                            currentType.GetProperty(currentValue)?.PropertyType;

                        if (type != null)
                        {
                            _resolvedTypes.Add(currentValue, type);
                            currentType = type;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Failed to find field or property with name '{currentValue}' in parent type '{currentType.FullName}'");
                        }
                    }
                }
                else
                {
                    FieldInfo? field = currentType.GetField(currentValue);
                    if (field != null)
                    {
                        return new ViewSource(field.FieldType, name, field);
                    }

                    PropertyInfo? property = currentType.GetProperty(currentValue);
                    if (property != null)
                    {
                        return new ViewSource(property.PropertyType, name, property);
                    }

                    throw new InvalidOperationException($"Failed to find field or property with name '{currentValue}' in parent type '{currentType.FullName}'");
                }
            }
        }
    }
}
