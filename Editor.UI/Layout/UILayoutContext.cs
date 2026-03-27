using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Layout
{
    public readonly record struct UILayoutContext(LayoutHandler Handler, UIMeasurements Measurements, Vector2 LocalRegion);
}
