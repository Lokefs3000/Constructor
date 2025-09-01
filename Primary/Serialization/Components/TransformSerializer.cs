using Arch.Core;
using Arch.Core.Extensions;
using Primary.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Serialization.Components
{
    internal record struct TransformSerializer : IComponentSerializer<Transform>
    {
        public bool Serialize(ref SDFWriter writer, ref Transform component)
        {
            writer.BeginObject("Transform");
            {
                writer.Write("Position", component.Position);
                writer.Write("Rotation", component.Rotation);
                writer.Write("Scale", component.Scale);
            }
            writer.EndObject();

            return true;
        }

        public bool Deserialize(ref SDFReader reader, Entity entity)
        {
            entity.Add(new Transform
            {
                Position = reader.Read<Vector3>("Position"),
                Rotation = reader.Read<Quaternion>("Rotation"),
                Scale = reader.Read<Vector3>("Scale")
            });

            return true;
        }
    }
}
