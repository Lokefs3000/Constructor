using System;
using System.Collections.Generic;
using System.Text;

namespace VoxelizationDemo.Editor.Serialization
{
    internal sealed class SerializationManager
    {
        // Databases
        private readonly PropertyDatabase _propertyDatabase;
        private readonly ConverterDatabase _converterDatabase;

        // Serializers
        private readonly ObjectSerializer _objectSerializer;

        internal SerializationManager()
        {
            _propertyDatabase = new PropertyDatabase();
            _converterDatabase = new ConverterDatabase();

            _objectSerializer = new ObjectSerializer(this);
        }

        internal PropertyDatabase PropertyDatabase => _propertyDatabase;
        internal ConverterDatabase ConverterDatabase => _converterDatabase;

        internal ObjectSerializer ObjectSerializer => _objectSerializer;
    }
}
