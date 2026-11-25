using System;
using System.Collections.Generic;
using System.Text;

namespace VoxelizationDemo.Editor.Serialization
{
    internal abstract class PropertyProvider
    {
        public abstract SerializationBag Properties { get; }
    }
}
