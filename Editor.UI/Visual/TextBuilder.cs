using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Visual
{
    public struct TextBuilder
    {
        private UITextAlignment _alignment;
        private UITextOverflow _overflow;

        private Vector2 _maxExtents;

        public TextBuilder()
        {
            _alignment = UITextAlignment.TopLeft;
            _overflow = UITextOverflow.Overflow;

            _maxExtents = Vector2.PositiveInfinity;
        }

        public TextBuilder SetAlignment(UITextAlignment alignment)
        {
            _alignment = alignment;
            return this;
        }

        public TextBuilder SetOverflow(UITextOverflow overflow)
        {
            _overflow = overflow;
            return this;
        }

        public TextBuilder SetMaxExtents(Vector2 maxExtents)
        {
            _maxExtents = maxExtents;
            return this;
        }

        internal RawTextBuilderData ToRaw() => new RawTextBuilderData(_alignment, _overflow, _maxExtents);

        public static readonly TextBuilder Default = new TextBuilder();
    }

    internal readonly record struct RawTextBuilderData(UITextAlignment Alignment, UITextOverflow Overflow, Vector2 MaxExtents);
}
