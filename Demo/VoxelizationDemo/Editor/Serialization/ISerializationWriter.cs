using System;
using System.Collections.Generic;
using System.Text;

namespace VoxelizationDemo.Editor.Serialization
{
    internal interface ISerializationWriter
    {
        public void WriteStartObject();
        public void WriteEndObject();

        public void WriteStartArray();
        public void WriteEndArray();
    }
}
