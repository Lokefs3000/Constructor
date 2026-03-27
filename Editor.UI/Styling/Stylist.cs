using Primary.Rendering.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Styling
{
    internal sealed class Stylist
    {
        private Dictionary<FastStringHash, StyledValue> _values;

        internal Stylist()
        {
            _values = new Dictionary<FastStringHash, StyledValue>();
        }
    }

    internal readonly record struct StyledValue(bool IsValueType, ushort Offset, object? Value);
}
