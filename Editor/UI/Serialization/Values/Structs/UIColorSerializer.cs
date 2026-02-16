using Editor.UI.Datatypes;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Values.Structs
{
    internal sealed class UIColorSerializer : ValueSerializer<UIColor>
    {
        public override string? Serialize(UIColor value)
        {
            if (value.Type == UIColorType.Gradient)
                return "Gradient";
            return ValueSerializerTable.Default.Serialize(value.Solid);
        }

        public override bool Deserialize(string value, out UIColor deserialized)
        {
            if (value == "Gradient")
            {
                deserialized = new UIGradientColor();
                return true;
            }

            if (ValueSerializerTable.Default.Deserialize(value, out Color color))
            {
                deserialized = color;
                return true;
            }
            else
            {
                deserialized = default;
                return false;
            }
        }
    }
}
