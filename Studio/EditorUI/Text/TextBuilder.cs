using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EditorUI.Text
{
    public record struct TextBuilder(Vector2 MaxExtents, TextWrapMode WrapMode = TextWrapMode.Overflow, TextAlignment Alignment = TextAlignment.TopLeft, bool AllowRichText = true)
    {
        public TextBuilder() : this(Vector2.PositiveInfinity, TextWrapMode.Overflow, TextAlignment.TopLeft, true)
        {
        }

        public TextBuilder(TextWrapMode wrapMode = TextWrapMode.Overflow, TextAlignment alignment = TextAlignment.TopLeft, bool allowRichText = true) : this(Vector2.PositiveInfinity, wrapMode, alignment, allowRichText)
        {
        }
    }
}
