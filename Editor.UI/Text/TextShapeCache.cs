using CommunityToolkit.Diagnostics;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Primary.Pooling;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.UI.Text
{
    internal sealed class TextShapeCache
    {
        private ObjectPool<ShapedTextData> _textDataPool;

        private Dictionary<TextShapeCacheKey, ShapedTextData> _cachedData;
        private HashSet<ShapedTextData> _returningData;

        private HashSet<TextShapeCacheKey> _unreferencedData;

        internal TextShapeCache()
        {
            _textDataPool = new ObjectPool<ShapedTextData>(new ShapedTextData.Policy());

            _cachedData = new Dictionary<TextShapeCacheKey, ShapedTextData>();
            _returningData = new HashSet<ShapedTextData>();

            _unreferencedData = new HashSet<TextShapeCacheKey>();
        }

        internal ShapedTextData GetBlankTextData()
        {
            ShapedTextData textData = _textDataPool.Get();
            _returningData.Add(textData);

            return textData;
        }

        internal void ReturnTextData(ShapedTextData textData)
        {
            _textDataPool.Return(textData);
        }

        internal void StoreDataInCache(TextShapeCacheKey key, ShapedTextData textData)
        {
            _cachedData.Add(key, textData);
            _returningData.Remove(textData);
        }

        internal bool TryFindInCache(TextShapeCacheKey key, [NotNullWhen(true)] out ShapedTextData? textData)
        {
            if (_cachedData.TryGetValue(key, out textData))
            {
                _unreferencedData.Remove(key);
                return true;
            }

            return false;
        }
    }

    internal readonly record struct TextShapeCacheKey(long StringHash, UIFontStyle Style, float BaseTextSize, float MaxWrapWidth, UITextOverflow Overflow)
    {
        public override int GetHashCode() => HashCode.Combine(StringHash, Style, BaseTextSize, MaxWrapWidth, Overflow);
    }
}
