using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Serialization.DataTypes
{
    internal struct Vector3Serializer : IDataTypeSerializer<Vector3>
    {
        public bool Serialize(ref SDFWriter writer, ref Vector3 component)
        {
            writer.BeginArray();
            {
                writer.Write(component.X);
                writer.Write(component.Y);
                writer.Write(component.Z);
            }
            writer.EndArray();

            return true;
        }

        public bool Deserialize(ref SDFReader reader, ref Vector3 component)
        {
            reader.BeginArray();
            {
                reader.Read(out component.X);
                reader.Read(out component.Y);
                reader.Read(out component.Z);
            }
            reader.EndArray();

            return true;
        }
    }
}
