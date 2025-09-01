using Arch.Core;
using Primary.Components;
using Primary.Serialization;
using Primary.Serialization.Components;
using Primary.Serialization.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Scenes
{
    internal static class SerializerTypes
    {
        private static Dictionary<string, ComponentSerializerWrapper> s_components = new Dictionary<string, ComponentSerializerWrapper>();
        private static Dictionary<Type, DataTypeSerializerWrapper> s_dataTypes = new Dictionary<Type, DataTypeSerializerWrapper>();

        internal static void RegisterDefault()
        {
            RegisterComponent(new TransformSerializer());

            RegisterDataType(new Vector3Serializer());
            RegisterDataType(new QuaternionSerializer());
        }

        private static unsafe void RegisterComponent<TComponent>(IComponentSerializer<TComponent> serializer) where TComponent : IComponent
        {
            IComponentSerializer<TComponent> data = serializer;
            bool r = s_components.TryAdd(typeof(TComponent).Name, new ComponentSerializerWrapper
            {
                Serialize = (sdf, comp) => data.Serialize(ref sdf, ref Unsafe.AsRef<TComponent>(comp.ToPointer())),
                Deserialize = (sdf, e) => data.Deserialize(ref sdf, e),
            });
        }

        private static unsafe void RegisterDataType<TDataType>(IDataTypeSerializer<TDataType> serializer)
        {
            IDataTypeSerializer<TDataType> data = serializer;
            bool r = s_dataTypes.TryAdd(typeof(TDataType), new DataTypeSerializerWrapper
            {
                Serialize = (sdf, comp) => data.Serialize(ref sdf, ref Unsafe.AsRef<TDataType>(comp.ToPointer())),
                //Deserialize = (sdf, comp) => data.Deserialize(ref sdf, out ),
            });
        }

        public static unsafe bool TrySerializeComponent<T>(SDFWriter writer, ref T component) where T : unmanaged, IComponent
        {
            if (s_components.TryGetValue(typeof(T).Name, out ComponentSerializerWrapper wrapper))
            {
                return wrapper.Serialize(writer, (nint)Unsafe.AsPointer(ref component));
            }

            return false;
        }

        public static unsafe bool TryDeserializeComponent(string typeName, SDFReader reader, Entity e)
        {
            if (s_components.TryGetValue(typeName, out ComponentSerializerWrapper wrapper))
            {
                return wrapper.Deserialize(reader, e);
            }

            return false;
        }

        public static unsafe bool TrySerializeDataType<T>(SDFWriter writer, ref T component)
        {
            if (s_dataTypes.TryGetValue(typeof(T), out DataTypeSerializerWrapper wrapper))
            {
                return wrapper.Serialize(writer, (nint)Unsafe.AsPointer(ref component));
            }

            return false;
        }

        public static unsafe bool TryDeserializeDataType<T>(SDFReader reader, out T dataType)
        {
            if (s_dataTypes.TryGetValue(typeof(T), out DataTypeSerializerWrapper wrapper))
            {
                bool v = wrapper.Deserialize(reader, out object obj);
                dataType = (T)obj;

                return v;
            }

            dataType = default!;
            return false;
        }

        private record struct ComponentSerializerWrapper
        {
            public Func<SDFWriter, nint, bool> Serialize;
            public Func<SDFReader, Entity, bool> Deserialize;
        }

        private record struct DataTypeSerializerWrapper
        {
            public Func<SDFWriter, nint, bool> Serialize;
            public DataTypeDeserialize Deserialize;
        }

        public delegate bool DataTypeDeserialize(SDFReader reader, out object obj);
    }
}
