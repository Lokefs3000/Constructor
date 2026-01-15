using Editor.UI.Assets;
using Editor.UI.Elements;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Text
{
    public struct UITextShapingInfo
    {
        public UIFontStyle Style;

        public string Text;
        public float Size;

        public float LetterSpacing;
        public float LineHeight;
        public UITextOverflow Overflow;

        public Vector2 MaxExtents;
    }
}
