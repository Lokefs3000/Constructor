using Editor.UI.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Editing
{
    public sealed class TextLineMetrics
    {
        private readonly UIFontTypeData _fontTypeData;

        public TextLineMetrics(UIFontTypeData typeData)
        {
            _fontTypeData = typeData;
        }

        public const int ChunkSize = 512;
    }

    public record struct TextMetricsChunk(int Length);
}
