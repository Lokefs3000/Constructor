using Editor.UI.Datatypes;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization.Values.Structs
{
    [ValueSerializerTarget(typeof(UIColor))]
    internal sealed class UIColorSerializer : IValueSerializer<UIColor>
    {
        public static string? Serialize(UIColor value)
        {
            if (value.Type == UIColorType.Gradient)
                return "Gradient";
            return UIManager.Instance.SerializationManager.ValueSerializerTable.Serialize(value.Solid);
        }

        public static bool Deserialize(string value, out UIColor deserialized)
        {
            if (value == "Gradient")
            {
                deserialized = new UIGradientColor();
                return true;
            }

            if (UIManager.Instance.SerializationManager.ValueSerializerTable.Deserialize(value, out Color color))
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
