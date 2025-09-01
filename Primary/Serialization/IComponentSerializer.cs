using Arch.Core;
using Primary.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Serialization
{
    public interface IComponentSerializer<T> where T : IComponent
    {
        public bool Serialize(ref SDFWriter writer, ref T component);
        public bool Deserialize(ref SDFReader reader, Entity entity);
    }
}
