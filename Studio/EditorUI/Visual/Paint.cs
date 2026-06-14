using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Common;
using Primary.Common;

namespace EditorUI.Visual
{
    public record struct Paint(UIColor Fill, UIColor Stroke, ushort StrokeWidth = 0, StrokePosition StrokePosition = StrokePosition.Outside)
    {
        public Paint(ushort StrokeWidth = 0, StrokePosition StrokePosition = StrokePosition.Outside) : this(Color.White, Color.Black, 0, StrokePosition.Outside)
        {
        }

        public Paint(UIColor Fill, ushort StrokeWidth = 0, StrokePosition StrokePosition = StrokePosition.Outside) : this(Fill, Color.Black, 0, StrokePosition.Outside)
        {
        }
    }

    public enum StrokePosition : byte
    {
        Outside = 0,
        Middle,
        Inner
    }
}
