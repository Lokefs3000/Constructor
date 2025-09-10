using Editor.DearImGui;
using Editor.PropertiesViewer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Inspector.Entities
{
    internal sealed class VSVector3 : ValueStorageBase
    {
        private Vector3 _value;

        public VSVector3(object @ref, ReflectionField field) : base(@ref, field) => _value = (Vector3)field.GetValue(@ref)!;
        public override bool Render(in string headerText)
        {
            if (ImGuiWidgets.InputVector3(headerText, ref _value))
            {
                _field.SetValue(_reference, _value);
                return true;
            }

            return false;
        }
    }
}
