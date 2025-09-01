using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Serialization.DataTypes
{
    internal struct QuaternionSerializer : IDataTypeSerializer<Quaternion>
    {
        public bool Serialize(ref SDFWriter writer, ref Quaternion component)
        {
            writer.BeginArray();
            {
                writer.Write(component.X);
                writer.Write(component.Y);
                writer.Write(component.Z);
                writer.Write(component.W);
            }
            writer.EndArray();

            return true;
        }

        public bool Deserialize(ref SDFReader reader, ref Quaternion component)
        {
            reader.BeginArray();
            {
                reader.Read(out component.X);
                reader.Read(out component.Y);
                reader.Read(out component.Z);
                reader.Read(out component.W);
            }
            reader.EndArray();

            return true;
        }
    }
}
