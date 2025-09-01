using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Editor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public sealed class InspectorHiddenAttribute : Attribute
    {
        public InspectorHiddenAttribute() { }
    }
}
