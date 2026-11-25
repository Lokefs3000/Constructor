using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Visual
{
    public struct TextBuilder
    {
        private TextOrigin _origin;

        private UITextAlignment _alignment;
        private UITextOverflow _overflow;

        private Vector2 _maxExtents;

        private bool _allowRichText;

        public TextBuilder()
        {
            _origin = TextOrigin.Top;

            _alignment = UITextAlignment.TopLeft;
            _overflow = UITextOverflow.Overflow;

            _maxExtents = Vector2.PositiveInfinity;

            _allowRichText = true;
        }

        public TextBuilder SetOrigin(TextOrigin origin)
        {
            _origin = origin;
            return this;
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

        public TextBuilder SetAllowRichText(bool allowRichText)
        {
            _allowRichText = allowRichText;
            return this;
        }

        internal RawTextBuilderData ToRaw() => new RawTextBuilderData(_origin, _alignment, _overflow, _maxExtents, _allowRichText);

        public static readonly TextBuilder Default = new TextBuilder();
    }

    internal readonly record struct RawTextBuilderData(TextOrigin Origin, UITextAlignment Alignment, UITextOverflow Overflow, Vector2 MaxExtents, bool AllowRichText);
}
