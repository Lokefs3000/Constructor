using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Text;

namespace EditorUI.Built
{
    public readonly record struct BuiltTextBuilder(Vector2 MaxExtents, TextWrapMode WrapMode, TextAlignment Alignment, bool AllowRichText)
    {
        public static BuiltTextBuilder Build(ref readonly TextBuilder textBuilder)
        {
            return new BuiltTextBuilder(
                textBuilder.MaxExtents,
                textBuilder.WrapMode,
                textBuilder.Alignment,
                textBuilder.AllowRichText);
        }
    }
}
