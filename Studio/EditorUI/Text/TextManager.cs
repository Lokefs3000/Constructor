using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Built;
using Primary.Pooling;

namespace EditorUI.Text
{
    public sealed class TextManager
    {
        private TextCache _textCache;
        private TextShaper _textShaper;

        internal TextManager()
        {
            _textCache = new TextCache();
            _textShaper = new TextShaper();
        }

        internal void ClearPreviousTextData()
        {
            _textCache.FinishAfterFrame();
        }

        public TextShapingData ShapeText(ReadOnlySpan<char> text, float fontSize, BuiltTextBuilder textBuilder, FontStyleData fontStyleData)
        {
            TextShapingData shapingData = _textCache.GetTemporaryShapingData(text);
            shapingData.SetDataKey(new ShapingDataKey(int.MinValue, textBuilder, fontStyleData, fontSize));

            shapingData.AllocateLettersFor(text.Length);
            _textShaper.Shape(text, fontSize, fontStyleData, textBuilder, shapingData);
            shapingData.CalculateMetrics();

            return shapingData;
        }

        public void GetOrShapeTextFor(object owner, [NotNull] ref TextShapingData? shapingData, ReadOnlySpan<char> text, BuiltTextBuilder textBuilder, FontStyleData styleData, float fontSize)
        {
            ShapingDataKey dataKey = new ShapingDataKey(text.GetDjb2HashCode(), textBuilder, styleData, fontSize);

            TextShapingData newShapingData = _textCache.GetNewOrCachedData(dataKey, owner, shapingData, out bool exists);

            if (!exists)
            {
                newShapingData.AllocateLettersFor(text.Length);
                _textShaper.Shape(text, fontSize, styleData, textBuilder, newShapingData);
                newShapingData.CalculateMetrics();
            }

            shapingData = newShapingData;
        }

        public void ForgetShapingDataFor(object owner)
        {
            _textCache.ReturnShapingDataFrom(owner);
        }

        public TextCache TextCache => _textCache;
    }
}
