using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using EditorUI.Built;
using EditorUI.Visual;
using Primary.Common;
using Primary.Mathematics;
using TerraFX.Interop.Windows;

namespace EditorUI.Text
{
    public sealed class TextShaper
    {
        private Stack<Paint> _paintStack;
        private Stack<FontStyleData> _styleDataStack;
        private Stack<float> _sizeStack;

        internal TextShaper()
        {
            _paintStack = new Stack<Paint>();
            _styleDataStack = new Stack<FontStyleData>();
            _sizeStack = new Stack<float>();
        }

        private void Clear()
        {
            _paintStack.Clear();
            _styleDataStack.Clear();
            _sizeStack.Clear();
        }

        internal void Shape(ReadOnlySpan<char> buffer, float fontSize, FontStyleData styleData, BuiltTextBuilder textBuilder, TextShapingData shapingData)
        {
            ScaledStyleMetrics metrics = ScaledStyleMetrics.Scale(styleData.Metrics, fontSize);
            ScaledStyleAdvances advances = ScaledStyleAdvances.Scale(styleData.Advances, styleData.Metrics.UnitsPerEm, fontSize);

            MutableTextVisual visual = new MutableTextVisual(null, fontSize, styleData);
            TextShapingVisual? bakedVisual = null;

            bool isRichTextEnabled = textBuilder.AllowRichText;

            FontGlyph? lastGlyph = null;
            FontGlyph? glyphAtLastWord = null;

            int currentLineStartIndex = 0;

            int wordBufferStartIndex = -1;
            float widthAtWordStart = 0.0f;
            float tallestGlyphAtWordStart = 0.0f;

            float currentWidth = 0.0f;
            float tallestGlyphInLine = 0.0f;
            float tallestGlyphInCurrentWord = 0.0f;

            float currentLeftOffset = 0.0f;

            int glyphsWithActualVisual = 0;

            // X = upper, Y = lower
            Vector2 lineVerticalOffsets = Vector2.Zero;
            float currentSectionLineOffset = 0;

            for (int i = 0; i < buffer.Length; ++i)
            {
                char letter = buffer[i];
                switch (letter)
                {
                    case '\n':
                        {
                            if (lastGlyph.HasValue)
                            {
                                ref FontGlyph lastGlyphRef = ref lastGlyph.DangerousGetValueOrNullReference();
                                currentWidth -= (lastGlyphRef.Advance - lastGlyphRef.Dimensions.X) * fontSize;
                            }

                            shapingData.AddLetters(buffer[currentLineStartIndex..i]);
                            BreakCurrentLine(shapingData, i, new Vector2(currentWidth, Math.Max(tallestGlyphInLine, tallestGlyphInCurrentWord)), currentSectionLineOffset + lineVerticalOffsets.X, currentLeftOffset, (bakedVisual ??= visual.Bake()), glyphsWithActualVisual);
                            ResetState();

                            lastGlyph = null;
                            currentLineStartIndex = i + 1;
                            break;
                        }
                    case '<':
                        {
                            if (!isRichTextEnabled)
                                goto default;

                            int previousIndex = i;
                            if (HandleRichText(buffer[i..], ref i, fontSize, styleData, ref visual, out MutableTextVisual newTextVisual, out RichTextEffect effect))
                            {
                                if (i > previousIndex)
                                {
                                    shapingData.AddLetters(buffer[currentLineStartIndex..previousIndex]);
                                    shapingData.AddSection(currentLeftOffset, (bakedVisual ??= visual.Bake()), glyphsWithActualVisual);

                                    currentLineStartIndex = i + 1;
                                }

                                currentLeftOffset = currentWidth;

                                if (effect.HasFlags(RichTextEffect.Disabled))
                                {
                                    isRichTextEnabled = false;
                                }
                                else
                                {
                                    if (effect.HasAny(RichTextEffect.Style | RichTextEffect.Size))
                                    {
                                        ScaledStyleMetrics newMetrics = ScaledStyleMetrics.Scale(newTextVisual.StyleData.Metrics, newTextVisual.PixelSize);
                                        Vector2 lineOffsets = new Vector2(
                                                metrics.Ascender - newMetrics.Ascender,
                                                -(metrics.Descender - newMetrics.Descender));

                                        lineVerticalOffsets = Vector2.Max(lineVerticalOffsets, lineOffsets);

                                        metrics = newMetrics;
                                        advances = ScaledStyleAdvances.Scale(styleData.Advances, newTextVisual.StyleData.Metrics.UnitsPerEm, newTextVisual.PixelSize);
                                    }

                                    visual = newTextVisual;
                                    bakedVisual = null;
                                }
                            }

                            break;
                        }
                    case ' ':
                        {
                            currentWidth += advances.SpaceAdvance;
                            wordBufferStartIndex = -1;
                            tallestGlyphInLine = Math.Max(tallestGlyphInLine, tallestGlyphInCurrentWord);
                            break;
                        }
                    default:
                        {
                            bool isControl = char.IsControl(letter);
                            bool isWordCompatible = !(isControl || char.IsWhiteSpace(letter));

                            if (!isWordCompatible)
                            {
                                wordBufferStartIndex = -1;
                                tallestGlyphInLine = Math.Max(tallestGlyphInLine, tallestGlyphInCurrentWord);
                            }

                            if (!char.IsControl(letter))
                            {
                                lastGlyph = visual.StyleData.FindGlyph(letter);
                                ref FontGlyph lastGlyphRef = ref lastGlyph.DangerousGetValueOrNullReference();

                                float nextWidth = currentWidth + lastGlyphRef.Advance * visual.PixelSize;

                                ++glyphsWithActualVisual;

                                if (isWordCompatible && wordBufferStartIndex == -1)
                                {
                                    wordBufferStartIndex = i;
                                    widthAtWordStart = currentWidth;
                                    tallestGlyphAtWordStart = tallestGlyphInLine;
                                    tallestGlyphInCurrentWord = 0.0f;
                                    glyphAtLastWord = lastGlyph;
                                }

                                switch (textBuilder.WrapMode)
                                {
                                    case TextWrapMode.Ellipsis:
                                        {
                                            if (nextWidth <= textBuilder.WrapWidth)
                                                break;

                                            FontGlyph ellipsisGlyph = visual.StyleData.FindGlyph('…');
                                            float glyphWidth = ellipsisGlyph.Dimensions.X * visual.PixelSize;

                                            for (int j = i; j >= 0; --j)
                                            {
                                                nextWidth = currentWidth + glyphWidth;
                                                if (nextWidth <= textBuilder.WrapWidth)
                                                {
                                                    shapingData.AddLetters(buffer[currentLineStartIndex..j]);
                                                    shapingData.AddLetter('…');

                                                    tallestGlyphInCurrentWord = Math.Max(tallestGlyphInCurrentWord, ellipsisGlyph.Dimensions.Y * visual.PixelSize);

                                                    BreakCurrentLine(shapingData, j + 1, new Vector2(nextWidth, Math.Max(tallestGlyphInLine, tallestGlyphInCurrentWord)), currentSectionLineOffset + lineVerticalOffsets.Y, currentLeftOffset, (bakedVisual ??= visual.Bake()), glyphsWithActualVisual);
                                                    break;
                                                }

                                                if (j == 0)
                                                    break;

                                                currentWidth -= visual.StyleData.FindGlyph(buffer[j]).Advance * visual.PixelSize;
                                                --glyphsWithActualVisual;
                                            }

                                            shapingData.SetRenderData(glyphsWithActualVisual);
                                            return;
                                        }
                                    case TextWrapMode.Wrap:
                                        {
                                            if (nextWidth <= textBuilder.WrapWidth)
                                                break;

                                            if (lastGlyph.HasValue)
                                            {
                                                lastGlyphRef = ref lastGlyph.DangerousGetValueOrNullReference();
                                                currentWidth -= (lastGlyphRef.Advance - lastGlyphRef.Dimensions.X) * fontSize;
                                            }

                                            if (wordBufferStartIndex == -1)
                                            {
                                                shapingData.AddLetters(buffer[currentLineStartIndex..i]);
                                                BreakCurrentLine(shapingData, i - 1, new Vector2(currentWidth, Math.Max(tallestGlyphInLine, tallestGlyphInCurrentWord)), currentSectionLineOffset + lineVerticalOffsets.Y, currentLeftOffset, (bakedVisual ??= visual.Bake()), glyphsWithActualVisual);
                                                ResetState();

                                                nextWidth -= currentWidth;
                                                lastGlyph = null;

                                                tallestGlyphInCurrentWord = 0.0f;
                                                tallestGlyphInLine = 0.0f;
                                            }
                                            else
                                            {
                                                // clip the word
                                                if (wordBufferStartIndex <= currentLineStartIndex)
                                                {
                                                    shapingData.AddLetters(buffer[currentLineStartIndex..i]);
                                                    BreakCurrentLine(shapingData, i - 1, new Vector2(currentWidth, Math.Max(tallestGlyphInLine, tallestGlyphInCurrentWord)), currentSectionLineOffset + lineVerticalOffsets.Y, currentLeftOffset, (bakedVisual ??= visual.Bake()), glyphsWithActualVisual);
                                                    ResetState();

                                                    lastGlyphRef = ref lastGlyph.DangerousGetValueOrNullReference();

                                                    nextWidth = lastGlyphRef.Advance * visual.PixelSize;
                                                    lastGlyph = null;

                                                    currentLineStartIndex = i;

                                                    wordBufferStartIndex = i;
                                                    widthAtWordStart = 0.0f;

                                                    tallestGlyphInCurrentWord = 0.0f;
                                                    tallestGlyphInLine = 0.0f;
                                                }
                                                else
                                                {
                                                    float currentWordWidth = nextWidth - widthAtWordStart;
                                                    if (glyphAtLastWord.HasValue)
                                                    {
                                                        ref FontGlyph glyphAtLastWordRef = ref glyphAtLastWord.DangerousGetValueOrNullReference();
                                                        widthAtWordStart -= (glyphAtLastWordRef.Advance - glyphAtLastWordRef.Dimensions.X) * visual.PixelSize;
                                                    }

                                                    shapingData.AddLetters(buffer[currentLineStartIndex..wordBufferStartIndex]);
                                                    BreakCurrentLine(shapingData, wordBufferStartIndex - 1, new Vector2(widthAtWordStart, tallestGlyphAtWordStart), currentSectionLineOffset + lineVerticalOffsets.Y, currentLeftOffset, (bakedVisual ??= visual.Bake()), glyphsWithActualVisual);

                                                    currentLineStartIndex = wordBufferStartIndex;

                                                    ResetState();

                                                    lastGlyph = null;

                                                    widthAtWordStart = 0.0f;
                                                    nextWidth = currentWordWidth;

                                                    tallestGlyphInLine = 0.0f;
                                                }
                                            }

                                            break;
                                        }
                                }

                                currentWidth = nextWidth;
                                if (lastGlyph.HasValue && !Unsafe.IsNullRef(in lastGlyphRef))
                                    tallestGlyphInLine = Math.Max(tallestGlyphInLine, lastGlyphRef.Dimensions.Y * fontSize);
                            }
                            else
                            {
                                lastGlyph = null;
                            }

                            break;
                        }
                }
            }

            {
                if (lastGlyph.HasValue)
                {
                    ref FontGlyph lastGlyphRef = ref lastGlyph.DangerousGetValueOrNullReference();
                    currentWidth -= (lastGlyphRef.Advance - lastGlyphRef.Dimensions.X) * fontSize;
                }

                tallestGlyphInLine = Math.Max(tallestGlyphInLine, tallestGlyphInCurrentWord);

                shapingData.AddLetters(buffer[currentLineStartIndex..buffer.Length]);
                shapingData.SetRenderData(glyphsWithActualVisual);
                BreakCurrentLine(shapingData, buffer.Length, new Vector2(currentWidth, tallestGlyphInLine), currentSectionLineOffset + lineVerticalOffsets.X, currentLeftOffset, (bakedVisual ??= visual.Bake()), glyphsWithActualVisual);
                ResetState();
            }

            Clear();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ResetState()
            {
                currentSectionLineOffset += metrics.LineHeight + Vector2.Sum(lineVerticalOffsets);

                wordBufferStartIndex = -1;

                currentWidth = 0.0f;

                tallestGlyphInLine = 0.0f;
                tallestGlyphInCurrentWord = 0.0f;

                lineVerticalOffsets = Vector2.Zero;
                currentLeftOffset = 0.0f;
            }
        }

        private void BreakCurrentLine(TextShapingData shapingData, int index, Vector2 lineSize, float yOffset, float currentLeftOffset, TextShapingVisual visual, int glyphsWithActualVisual)
        {
            shapingData.AddSection(currentLeftOffset, visual, glyphsWithActualVisual);
            shapingData.AddLine(lineSize, yOffset);
        }

        private bool HandleRichText(ReadOnlySpan<char> buffer, ref int textIndex, float defaultPixelSize, FontStyleData defaultStyleData, ref MutableTextVisual currentVisual, out MutableTextVisual newTextVisual, out RichTextEffect effect)
        {
            newTextVisual = default;

            RichTextEffect removed = RichTextEffect.None;
            effect = RichTextEffect.None;

            newTextVisual = currentVisual;

            int lastIndex = 0;

            using RichTextParser parser = new RichTextParser(buffer);
            while (parser.MoveNext())
            {
                if (parser.Current.Length == 1 && parser.Current[0] == '>')
                    goto ExitSuccess;

                ReadOnlySpan<char> symbolName = parser.Current;
                if (symbolName.IsEmpty)
                    return false;

                if (symbolName[0] == '/')
                {
                    RichTextSymbol symbol = RichTextDatabase.GetSymbol(symbolName.Slice(1));
                    switch (symbol)
                    {
                        case RichTextSymbol.Color:
                            {
                                if (_paintStack.TryPop(out Paint _))
                                {
                                    newTextVisual.Paint = _paintStack.TryPeek(out Paint result) ? result : null;
                                    removed |= RichTextEffect.Color;
                                }
                                else
                                    return false;

                                break;
                            }
                        case RichTextSymbol.Bold:
                        case RichTextSymbol.Italic:
                        case RichTextSymbol.BoldItalic:
                        case RichTextSymbol.Style:
                            {
                                if (_styleDataStack.TryPop(out FontStyleData? _))
                                {
                                    newTextVisual.StyleData = _styleDataStack.TryPeek(out FontStyleData? result) ? result : defaultStyleData;
                                    removed |= RichTextEffect.Style;
                                }
                                else
                                    return false;

                                break;
                            }
                        case RichTextSymbol.Size:
                            {
                                if (_sizeStack.TryPop(out float _))
                                {
                                    newTextVisual.PixelSize = _sizeStack.TryPeek(out float result) ? result : defaultPixelSize;
                                    removed |= RichTextEffect.Size;
                                }
                                else
                                    return false;

                                break;
                            }
                        case RichTextSymbol.Disable: return false;
                        case RichTextSymbol.Break: return false;
                        default: return false;
                    }
                }
                else
                {
                    RichTextSymbol symbol = RichTextDatabase.GetSymbol(symbolName);
                    switch (symbol)
                    {
                        case RichTextSymbol.Color:
                            {
                                if (!parser.MoveNext() || parser.Current.IsEmpty)
                                    return false;

                                Color? color = null;

                                ReadOnlySpan<char> value = parser.Current;
                                if (value[0] == '#')
                                {
                                    value = value.Slice(1);

                                    switch (value.Length)
                                    {
                                        case 2:
                                            {
                                                if (byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte result))
                                                    color = new Color(result * (1.0f / 255.0f));
                                                else
                                                    return false;
                                                break;
                                            }
                                        case 4:
                                            {
                                                if (ushort.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort result))
                                                {
                                                    Vector2 v2 = new Vector2((result >> 8) & 0xff, result & 0xff) * (1.0f / 255.0f);
                                                    color = new Color(v2.X, v2.X, v2.X, v2.Y);
                                                }
                                                else
                                                    return false;
                                                break;
                                            }
                                        case 6:
                                            {
                                                if (uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint result))
                                                {
                                                    Vector3 v3 = new Vector3((result >> 16) & 0xff, (result >> 8) & 0xff, result & 0xff) * (1.0f / 255.0f);
                                                    color = new Color(v3.X, v3.Y, v3.Z);
                                                }
                                                else
                                                    return false;
                                                break;
                                            }
                                        case 8:
                                            {
                                                if (uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint result))
                                                {
                                                    Vector4 v4 = new Vector4((result >> 24) & 0xff, (result >> 16) & 0xff, (result >> 8) & 0xff, result & 0xff) * (1.0f / 255.0f);
                                                    color = new Color(v4.X, v4.Y, v4.Z, v4.W);
                                                }
                                                else
                                                    return false;
                                                break;
                                            }
                                        default: return false;
                                    }
                                }
                                else
                                {
                                    var tokenizer = value.Tokenize(',');

                                    Color c = Color.TransparentBlack;

                                    if (tokenizer.MoveNext() && byte.TryParse(tokenizer.Current, out byte result))
                                        c.A = result * (1.0f / 255.0f);
                                    else
                                        return false;

                                    if (tokenizer.MoveNext())
                                    {
                                        if (byte.TryParse(tokenizer.Current, out result))
                                        {
                                            Unsafe.As<Color, Vector3>(ref c) = new Vector3(c.A);
                                            c.A = result * (1.0f / 255.0f);
                                        }
                                        else
                                            return false;
                                    }

                                    if (tokenizer.MoveNext())
                                    {
                                        if (byte.TryParse(tokenizer.Current, out result))
                                        {
                                            c.G = c.A;
                                            c.B = result * (1.0f / 255.0f);
                                            c.A = 1.0f;
                                        }
                                        else
                                            return false;
                                    }

                                    if (tokenizer.MoveNext())
                                    {
                                        if (byte.TryParse(tokenizer.Current, out result))
                                        {
                                            c.A = result * (1.0f / 255.0f);
                                        }
                                        else
                                            return false;
                                    }

                                    if (tokenizer.MoveNext())
                                        return false;
                                }

                                if (color.HasValue)
                                {
                                    Paint paint = new Paint(color.Value);
                                    newTextVisual.Paint = paint;
                                    effect |= RichTextEffect.Color;
                                }
                                else
                                    return false;

                                break;
                            }
                        case RichTextSymbol.Bold:
                            {
                                newTextVisual.StyleData = defaultStyleData.Family.GetFontStyle(currentVisual.StyleData.Style, FontWeight.Bold) ?? defaultStyleData;
                                effect |= RichTextEffect.Style;
                                break;
                            }
                        case RichTextSymbol.Italic:
                            {
                                newTextVisual.StyleData = defaultStyleData.Family.GetFontStyle(FontStyle.Italic, currentVisual.StyleData.Weight) ?? defaultStyleData;
                                effect |= RichTextEffect.Style;
                                break;
                            }
                        case RichTextSymbol.BoldItalic:
                            {
                                newTextVisual.StyleData = defaultStyleData.Family.GetFontStyle(FontStyle.Italic, FontWeight.Bold) ?? defaultStyleData;
                                effect |= RichTextEffect.Style;
                                break;
                            }
                        case RichTextSymbol.Style:
                            {
                                if (!parser.MoveNext())
                                    return false;

                                var tokenizer = parser.Current.Tokenize(' ');
                                if (!tokenizer.MoveNext())
                                    return false;

                                FontStyle style = FontStyle.Normal;
                                FontWeight weight = FontWeight.Normal;

                                if (tokenizer.Current.Length == 3)
                                {
                                    int target = tokenizer.Current[0] - '1';
                                    if (target < (int)FontWeight.w100 || target > (int)FontWeight.w900)
                                        return false;
                                }

                                if (tokenizer.MoveNext())
                                {
                                    if (tokenizer.Current.SequenceEqual("Normal"))
                                        style = FontStyle.Normal;
                                    else if (tokenizer.Current.SequenceEqual("Italic"))
                                        style = FontStyle.Italic;
                                    else
                                        return false;

                                    if (tokenizer.MoveNext())
                                        return false;
                                }

                                newTextVisual.StyleData = currentVisual.StyleData.Family.GetFontStyle(style, weight) ?? defaultStyleData;
                                effect |= RichTextEffect.Style;
                                break;
                            }
                        case RichTextSymbol.Size:
                            {
                                if (!parser.MoveNext() || !float.TryParse(parser.Current, CultureInfo.InvariantCulture, out float result))
                                    return false;

                                newTextVisual.PixelSize = result;
                                effect |= RichTextEffect.Size;

                                break;
                            }
                        case RichTextSymbol.Disable: effect |= RichTextEffect.Disabled; break;
                        case RichTextSymbol.Break: break;
                        default: return false;
                    }
                }

                lastIndex = parser.Index;
            }

            return false;

        ExitSuccess:
            RichTextEffect combined = effect | removed;
            if (combined == RichTextEffect.None)
                return false;

            if (Flags.HasFlag(effect, RichTextEffect.Color))
                _paintStack.Push(newTextVisual.Paint!.Value);
            if (Flags.HasFlag(effect, RichTextEffect.Style))
                _styleDataStack.Push(newTextVisual.StyleData);
            if (Flags.HasFlag(effect, RichTextEffect.Size))
                _sizeStack.Push(newTextVisual.PixelSize);

            textIndex += lastIndex;
            effect = combined;

            return true;
        }

        private readonly record struct ScaledStyleMetrics(float Ascender, float Descender, float LineHeight, float UnderlineY, float Height)
        {
            public static ScaledStyleMetrics Scale(FontStyleMetrics metrics, float multiplier)
            {
                float unitsPerEm = metrics.UnitsPerEm * multiplier;
                return new ScaledStyleMetrics(metrics.Ascender * unitsPerEm, metrics.Descender * unitsPerEm, metrics.LineHeight * unitsPerEm, metrics.UnderlineY * unitsPerEm, metrics.Height * unitsPerEm);
            }
        }

        private readonly record struct ScaledStyleAdvances(float SpaceAdvance, float TabAdvance)
        {
            public static ScaledStyleAdvances Scale(FontStyleAdvances advances, float unitsPerEm, float multiplier)
            {
                unitsPerEm *= multiplier;
                return new ScaledStyleAdvances(advances.Space * unitsPerEm, advances.Tab * unitsPerEm);
            }
        }

        private enum RichTextEffect : byte
        {
            None = 0,

            Color = 1 << 0,
            Style = 1 << 1,
            Size = 1 << 2,
            Disabled = 1 << 3,

            Removed = 1 << 7
        }
    }

    public record struct MutableTextVisual(Paint? Paint, float PixelSize, FontStyleData StyleData)
    {
        public TextShapingVisual Bake()
        {
            if (Paint.HasValue)
            {
                Paint paint = Paint.Value;
                return new TextShapingVisual(BuiltPaint.Build(in paint, UIManager.Instance.VisualManager.GradientManager), PixelSize, StyleData);
            }
            else
            {
                return new TextShapingVisual(null, PixelSize, StyleData);
            }
        }
    }
}
