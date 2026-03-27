using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Editor.UI.Assets;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Editor.UI.Text
{
    internal abstract class TextWrapper
    {
        private Stack<Color> _colorStack;
        private Stack<UIFontStyle> _styleStack;
        private Stack<float> _sizeStack;

        public TextWrapper()
        {
            _colorStack = new Stack<Color>();
            _styleStack = new Stack<UIFontStyle>();
            _sizeStack = new Stack<float>();
        }

        internal void Clear()
        {
            _colorStack.Clear();
            _styleStack.Clear();
            _sizeStack.Clear();
        }

        protected bool HandleRichText(ReadOnlySpan<char> slicedText, ref readonly TextWrapInfo info, ref int textIndex, MutableTextVisualInfo currentVisualInfo, out MutableTextVisualInfo newVisualInfo, out RichTextEffect effect)
        {
            RichTextEffect removed = RichTextEffect.None;

            effect = RichTextEffect.None;
            newVisualInfo = currentVisualInfo;

            int lastIndex = 0;

            using RichTextParser parser = new RichTextParser(slicedText);
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
                                if (_colorStack.TryPop(out Color _))
                                {
                                    newVisualInfo.DrawColor = new PaintColor(_colorStack.TryPeek(out Color result) ? result : info.DefaultVisualInfo.DrawColor.Solid);
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
                                if (_styleStack.TryPop(out UIFontStyle? _))
                                {
                                    newVisualInfo.Style = _styleStack.TryPeek(out UIFontStyle? result) ? result : info.DefaultVisualInfo.Style;
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
                                    newVisualInfo.FontSize = _sizeStack.TryPeek(out float result) ? result : info.DefaultVisualInfo.FontSize;
                                    removed |= RichTextEffect.Size;
                                }
                                else
                                    return false;

                                break;
                            }
                        case RichTextSymbol.Disable:
                            break;
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
                                    ReadOnlySpanTokenizer<char> tokenizer = value.Tokenize(',');

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
                                    newVisualInfo.DrawColor = new PaintColor(color.Value);
                                    effect |= RichTextEffect.Color;
                                }
                                else
                                    return false;

                                break;
                            }
                        case RichTextSymbol.Bold:
                            {
                                newVisualInfo.Style = currentVisualInfo.Style.Font.FindStyle("Bold") ?? currentVisualInfo.Style;
                                effect |= RichTextEffect.Style;
                                break;
                            }
                        case RichTextSymbol.Italic:
                            {
                                newVisualInfo.Style = currentVisualInfo.Style.Font.FindStyle("Italic") ?? currentVisualInfo.Style;
                                effect |= RichTextEffect.Style;
                                break;
                            }
                        case RichTextSymbol.BoldItalic:
                            {
                                newVisualInfo.Style = currentVisualInfo.Style.Font.FindStyle("BoldItalic") ?? currentVisualInfo.Style;
                                effect |= RichTextEffect.Style;
                                break;
                            }
                        case RichTextSymbol.Style:
                            {
                                if (!parser.MoveNext())
                                    return false;

                                newVisualInfo.Style = currentVisualInfo.Style.Font.FindStyle(parser.Current.GetDjb2HashCode()) ?? currentVisualInfo.Style;
                                effect |= RichTextEffect.Style;
                                break;
                            }
                        case RichTextSymbol.Size:
                            {
                                if (!parser.MoveNext() || !float.TryParse(parser.Current, CultureInfo.InvariantCulture, out float result))
                                    return false;

                                newVisualInfo.FontSize = result;
                                effect |= RichTextEffect.Size;

                                break;
                            }
                        case RichTextSymbol.Disable:
                            break;
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
                _colorStack.Push(newVisualInfo.DrawColor.Solid);
            if (Flags.HasFlag(effect, RichTextEffect.Style))
                _styleStack.Push(newVisualInfo.Style);
            if (Flags.HasFlag(effect, RichTextEffect.Size))
                _sizeStack.Push(newVisualInfo.FontSize);

            textIndex += lastIndex;
            effect = combined;

            return true;
        }

        public abstract void WrapText(in TextWrapInfo wrapInfo, ShapedTextData textData, Span<char> text);

        public const bool UseHardwareAcceleration = true; //temporary
    }

    internal enum RichTextEffect : byte
    {
        None = 0,

        Color = 1 << 0,
        Style = 1 << 1,
        Size = 1 << 2,

        Removed = 1 << 7
    }

    internal readonly record struct FontMetrics
    {
        public readonly float RelativeScale;
        public readonly float LineHeight;
        public readonly float Ascender;
        public readonly float Descender;

        public FontMetrics(UIFontStyle fontStyle, float emSize)
        {
            RelativeScale = emSize;
            Unsafe.As<FontMetrics, Vector3>(ref Unsafe.AddByteOffset(ref this, Unsafe.SizeOf<float>())) = new Vector3(
                fontStyle.Metrics.LineHeight,
                fontStyle.Metrics.Ascender,
                fontStyle.Metrics.Descender) * RelativeScale;
        }
    }

    public readonly record struct TextWrapInfo(Vector2 MaxExtents, TextVisualInfo DefaultVisualInfo);
}
