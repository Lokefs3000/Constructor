using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Text
{
    public static class RichTextDatabase
    {
        public static RichTextSymbol GetSymbol(ReadOnlySpan<char> symbolName)
        {
            if (s_symbolsLookup.TryGetValue(symbolName.GetDjb2HashCode(), out RichTextSymbol value))
                return value;
            return RichTextSymbol.Unknown;
        }

        private static FrozenDictionary<int, RichTextSymbol> s_symbolsLookup = new Dictionary<int, RichTextSymbol>()
        {
            { "color".GetDjb2HashCode(), RichTextSymbol.Color },
            { "b".GetDjb2HashCode(), RichTextSymbol.Bold },
            { "i".GetDjb2HashCode(), RichTextSymbol.Italic },
            { "bi".GetDjb2HashCode(), RichTextSymbol.BoldItalic },
            { "style".GetDjb2HashCode(), RichTextSymbol.Style },
            { "size".GetDjb2HashCode(), RichTextSymbol.Size },
            { "disable".GetDjb2HashCode(), RichTextSymbol.Disable },
            { "br".GetDjb2HashCode(), RichTextSymbol.Break },
        }.ToFrozenDictionary();
    }

    public enum RichTextSymbol : byte
    {
        Unknown = 0,

        Color,
        Bold,
        Italic,
        BoldItalic,
        Style,
        Size,
        Disable,
        Break
    }
}
