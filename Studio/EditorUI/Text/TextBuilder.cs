using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EditorUI.Text
{
    public record struct TextBuilder(float WrapWidth, TextWrapMode WrapMode = TextWrapMode.Overflow, TextAlignment Alignment = TextAlignment.TopLeft, bool AllowRichText = true)
    {
        public TextBuilder() : this(float.PositiveInfinity, TextWrapMode.Overflow, TextAlignment.TopLeft, true)
        {
        }

        public TextBuilder(TextWrapMode wrapMode = TextWrapMode.Overflow, TextAlignment alignment = TextAlignment.TopLeft, bool allowRichText = true) : this(float.PositiveInfinity, wrapMode, alignment, allowRichText)
        {
        }
    }
}
