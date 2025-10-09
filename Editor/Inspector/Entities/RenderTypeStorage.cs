using Editor.DearImGui;
using Primary.Assets;
using Primary.Rendering.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Inspector.Entities
{
    internal sealed class VSRawRenderMesh : ValueStorageBase
    {
        private RawRenderMesh? _value;
        private RawRenderMesh? _temp;

        public VSRawRenderMesh(ReflectionField field) : base(field) { }
        public override object? Render(in string headerText, ref object @ref)
        {
            _value = _field.GetValue(@ref) as RenderMesh;

            ImGuiWidgets.SelectorRawRenderMesh(headerText, _value, (x) => _temp = x);
            if (_temp != null)
            {
                _field.SetValue(@ref, _temp);
                _temp = null;

                return @ref;
            }

            return null;
        }
    }
}
