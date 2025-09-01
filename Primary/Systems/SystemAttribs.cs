using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Systems
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SystemRequirements : Attribute
    {
        private Type[] _requirements;

        public SystemRequirements(Type[] requirements)
        {
            _requirements = requirements;
        }

        public Type[] Requirements => _requirements;
    }
}
