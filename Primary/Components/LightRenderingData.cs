using Primary.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Components
{
    [ComponentUsage(CanBeAdded: false)]
    [InspectorHidden]
    public record struct LightRenderingData : IComponent
    {
        private bool _isDirty;
        private uint _lightIndex;
        private uint _shadowIndex;

        public LightRenderingData()
        {
            _isDirty = true;
            _lightIndex = uint.MaxValue;
            _shadowIndex = uint.MaxValue;
        }

        internal bool IsDirty { get => _isDirty; set => _isDirty = value; }
        internal uint LightIndex { get => _lightIndex; set => _lightIndex = value; }
        internal uint ShadowIndex { get => _shadowIndex; set => _shadowIndex = value; }
    }
}
