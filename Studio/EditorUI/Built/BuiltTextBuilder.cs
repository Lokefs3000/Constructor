using EditorUI.Text;

namespace EditorUI.Built
{
    public readonly record struct BuiltTextBuilder(float WrapWidth, TextWrapMode WrapMode, TextAlignment Alignment, bool AllowRichText)
    {
        public override int GetHashCode()
        {
            return HashCode.Combine(WrapWidth, WrapMode, Alignment, AllowRichText);
        }

        public static BuiltTextBuilder Build(ref readonly TextBuilder textBuilder)
        {
            return new BuiltTextBuilder(
                textBuilder.WrapWidth,
                textBuilder.WrapMode,
                textBuilder.Alignment,
                textBuilder.AllowRichText);
        }
    }
}
