using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Text;
using Primary.Collections;

namespace VoxelizationDemo.Editor.Serialization
{
    internal sealed class ReflectionPropertyProvider : PropertyProvider
    {
        private readonly Type _type;
        private readonly SerializationBag _bag;

        internal ReflectionPropertyProvider(Type type)
        {
            _type = type;

            using RentedList<SerializationProperty> properties = new RentedList<SerializationProperty>();
            
            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (fieldInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                    continue;

                bool shouldSerializeField = fieldInfo.IsPublic;
                if (fieldInfo.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                {
                    if (fieldInfo.Name.EndsWith("k__BackingField"))
                    {
                        shouldSerializeField = true;
                    }
                    else
                    {
                        shouldSerializeField = false;
                    }
                }

                if (!shouldSerializeField)
                {
                    // TODO: force serialize field
                    //  --Implementation--..
                }

                if (!shouldSerializeField)
                    continue;

                properties.Add(fieldInfo);
            }

            foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                MethodInfo? getMethod = propertyInfo.GetMethod;
                MethodInfo? setMethod = propertyInfo.SetMethod;

                if (getMethod == null || setMethod == null)
                    continue;

                if (propertyInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
                    continue;

                bool shouldSerializeField = getMethod.IsPublic && setMethod.IsPublic;
                if (!shouldSerializeField)
                {
                    // TODO: force serialize field
                    //  --Implementation--..
                }

                FieldInfo? autoImplementedField = type.GetField($"<{propertyInfo.Name}>k__BackingField");
                if (autoImplementedField != null)
                {
                    if (properties.Contains(autoImplementedField))
                        shouldSerializeField = false;
                }

                if (!shouldSerializeField)
                    continue;

                properties.Add(propertyInfo);
            }

            _bag = new SerializationBag([.. properties]);
        }

        public override SerializationBag Properties => _bag;
    }
}
