using Microsoft.Extensions.ObjectPool;

namespace Editor.UI.Text.Wrappers
{
    internal sealed class WordTextWrapper : TextWrapper
    {
        public override void WrapText(in TextWrapInfo wrapInfo, ShapedTextData textData, Span<char> text)
        {
            

            throw new NotSupportedException();
        }

        internal readonly record struct Policy : IPooledObjectPolicy<WordTextWrapper>
        {
            public WordTextWrapper Create() => new WordTextWrapper();
            public bool Return(WordTextWrapper obj) => true;
        }
    }
}
