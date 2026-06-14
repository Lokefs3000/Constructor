using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Editor.UI.Assets;
using Editor.UI.Visual;
using Microsoft.Extensions.ObjectPool;
using Primary.Common;
using Primary.Pooling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.UI.Text.Wrappers
{
    internal sealed class OverflowTextWrapper : TextWrapper
    {
        public OverflowTextWrapper()
        {
            Guard.IsTrue(Vector128.IsHardwareAccelerated);
            Guard.IsTrue(Vector256.IsHardwareAccelerated);
        }

        public unsafe override void WrapText(in TextWrapInfo wrapInfo, ShapedTextData textData, Span<char> text)
        {
            FontMetrics defaultMetrics = new FontMetrics(wrapInfo.DefaultVisualInfo.TypeData, wrapInfo.DefaultVisualInfo.FontSize);
            FontMetrics currentMetrics = defaultMetrics;

            TextVisualInfo defaultVisualInfo = new TextVisualInfo(wrapInfo.DefaultVisualInfo.DrawColor, defaultMetrics.RelativeScale, wrapInfo.DefaultVisualInfo.TypeData);
            MutableTextVisualInfo currentVisualInfo = defaultVisualInfo;

            bool richTextEnabled = wrapInfo.AllowRichText;

            int currentLineIndex = 0;
            float currentLineOffset = 0.0f;

            if (wrapInfo.Origin == TextOrigin.Top)
                currentLineOffset = 0.0f;
            else
                currentLineOffset = 0.0f;

            float baseLineOffset = wrapInfo.DefaultVisualInfo.TypeData.Metrics.LineHeight * defaultMetrics.RelativeScale;

            char lastValidAdvanceLetter = '\0';
            float currentWidth = 0.0f;

            // X = upper, Y = lower
            Vector2 lineVerticalOffsets = Vector2.Zero;
            float currentSectionLineOffset = 0.0f;

            Vector256<float> advanceVector = Vector256<float>.Zero;
            Span<float> advanceVectorSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<Vector256<float>, float>(ref advanceVector), 8);
            int advanceVectorCounter = 0;

            float maxHorizontalExtent = 0.0f;

            for (int i = 0; i < text.Length; i++)
            {
                char letter = text[i];
                switch (letter)
                {
                    case '\0':
                        {
                            if (i != text.Length - 1)
                                goto default;

                            if (i > 0)
                            {
                                if (lastValidAdvanceLetter != '\0' && advanceVectorCounter > 0)
                                {
                                    UIGlyph glyph = currentVisualInfo.TypeData.RequestGlyph(lastValidAdvanceLetter);
                                    advanceVectorSpan[advanceVectorCounter - 1] = glyph.Size.X;
                                }

                                if (advanceVectorCounter == 8)
                                    currentWidth += Vector256.Sum(advanceVector) * currentMetrics.RelativeScale;
                                else
                                    currentWidth += GetTextWidth(ref advanceVector);
                                maxHorizontalExtent = MathF.Max(maxHorizontalExtent, currentWidth);

                                textData.AddLetters(text[..i]);
                                textData.AddLine(currentLineIndex, (currentLineOffset + lineVerticalOffsets.X) * TextManager.PixelsPerEM, new Vector2(currentWidth, 0.0f) * TextManager.PixelsPerEM);
                                textData.AddSection(currentVisualInfo.TypeData, currentSectionLineOffset, currentVisualInfo);
                            }
                            else
                            {
                                textData.AddLine(currentLineIndex, (currentLineOffset + lineVerticalOffsets.X) * TextManager.PixelsPerEM, new Vector2(currentWidth, 0.0f) * TextManager.PixelsPerEM, true);
                            }

                            textData.SetMetrics(new Vector2(maxHorizontalExtent, currentLineOffset + baseLineOffset) * TextManager.PixelsPerEM);

                            return;
                        }
                    case '<':
                        {
                            if (!richTextEnabled)
                                goto default;

                            int prevIndex = i;
                            if (HandleRichText(text[i..], in wrapInfo, ref i, currentVisualInfo, out MutableTextVisualInfo newVisualInfo, out RichTextEffect effect))
                            {
                                if (i > prevIndex)
                                {
                                    textData.AddLetters(text[.. prevIndex]);
                                    textData.AddSection(currentVisualInfo.TypeData, currentSectionLineOffset, currentVisualInfo);

                                    text = text[(i + 1)..];
                                    i = -1;
                                }

                                if (advanceVectorCounter > 0)
                                {
                                    if (advanceVectorCounter == 8)
                                        currentWidth += Vector256.Sum(advanceVector) * currentMetrics.RelativeScale;
                                    else
                                        currentWidth += GetTextWidth(ref advanceVector);
                                    advanceVectorCounter = 0;
                                }

                                currentSectionLineOffset = currentWidth * TextManager.PixelsPerEM;

                                if (Flags.HasFlag(effect, RichTextEffect.Disabled))
                                {
                                    richTextEnabled = false;
                                }
                                else
                                {
                                    if (Flags.HasEither(effect, RichTextEffect.Style | RichTextEffect.Size))
                                    {
                                        FontMetrics newMetrics = new FontMetrics(newVisualInfo.TypeData, newVisualInfo.FontSize);
                                        if (currentVisualInfo.FontSize != newVisualInfo.FontSize)
                                        {
                                            Vector2 lineOffsets = new Vector2(
                                                currentMetrics.Ascender - newMetrics.Ascender,
                                                -(currentMetrics.Descender - newMetrics.Descender));

                                            lineVerticalOffsets = Vector2.Max(lineVerticalOffsets, lineOffsets);
                                        }

                                        currentMetrics = newMetrics;
                                    }

                                    currentVisualInfo = newVisualInfo;
                                }
                            }

                            break;
                        }
                    case '\n':
                        {
                            if (i > 0 && lastValidAdvanceLetter != '\0')
                            {
                                if (advanceVectorCounter > 0)
                                {
                                    UIGlyph glyph = currentVisualInfo.TypeData.RequestGlyph(lastValidAdvanceLetter);
                                    advanceVectorSpan[advanceVectorCounter - 1] = glyph.Size.X;

                                    if (advanceVectorCounter == 8)
                                        currentWidth += Vector256.Sum(advanceVector) * currentMetrics.RelativeScale;
                                    else
                                        currentWidth += GetTextWidth(ref advanceVector);

                                    advanceVectorCounter = 0;
                                }
                                else
                                {
                                    UIGlyph glyph = currentVisualInfo.TypeData.RequestGlyph(lastValidAdvanceLetter);
                                    currentWidth -= (glyph.Advance - glyph.Size.X) * currentMetrics.RelativeScale;
                                }
                                
                                maxHorizontalExtent = MathF.Max(maxHorizontalExtent, currentWidth);

                                textData.AddLetters(text[..i]);
                                textData.AddLine(currentLineIndex, (currentLineOffset + lineVerticalOffsets.X) * TextManager.PixelsPerEM, new Vector2(currentWidth, 0.0f) * TextManager.PixelsPerEM);
                                textData.AddSection(currentVisualInfo.TypeData, currentSectionLineOffset, currentVisualInfo);
                            }
                            else if (lastValidAdvanceLetter != '\0')
                            {
                                textData.AddLine(currentLineIndex, (currentLineOffset + lineVerticalOffsets.X) * TextManager.PixelsPerEM, new Vector2(currentWidth, 0.0f) * TextManager.PixelsPerEM, true);
                            }

                            currentLineOffset += currentMetrics.LineHeight + Vector2.Sum(lineVerticalOffsets);
                            ++currentLineIndex;

                            lastValidAdvanceLetter = '\0';
                            currentWidth = 0.0f;

                            lineVerticalOffsets = Vector2.Zero;
                            currentSectionLineOffset = 0.0f;

                            text = text[(i + 1)..];
                            i = -1;

                            break;
                        }
                    default:
                        {
                            if (!char.IsControl(letter))
                            {
                                UIGlyph glyph = currentVisualInfo.TypeData.RequestGlyph(letter);

                                if (advanceVectorCounter == 8)
                                {
                                    currentWidth += Vector256.Sum(advanceVector) * currentMetrics.RelativeScale;

                                    advanceVectorSpan[0] = glyph.Advance;
                                    advanceVectorCounter = 1;
                                }
                                else
                                    advanceVectorSpan[advanceVectorCounter++] = glyph.Advance;

                                lastValidAdvanceLetter = letter;
                            }

                            break;
                        }
                }
            }

            throw new UnreachableException();

            //slower but all inclusive method for getting width
            //NOTE: 'vector' never gets moved because it is local and therefore on the stack and not the heap
            float GetTextWidth(ref Vector256<float> vector)
            {
                switch (advanceVectorCounter)
                {
                    case 0: return 0.0f;
                    case 1: return advanceVector[0] * currentMetrics.RelativeScale;
                    case 2:
                        {
                            ((Vector2*)Unsafe.AsPointer(ref vector))[1] = Vector2.Zero;
                            goto case 4;
                        }
                    case 3:
                        {
                            ((float*)Unsafe.AsPointer(ref vector))[3] = 0.0f;
                            goto case 4;
                        }
                    case 4: return Vector128.Sum(advanceVector.GetLower()) * currentMetrics.RelativeScale;
                    case 5: return (Vector128.Sum(advanceVector.GetLower()) + advanceVector[4]) * currentMetrics.RelativeScale;
                    case 6:
                        {
                            ((Vector2*)Unsafe.AsPointer(ref vector))[3] = Vector2.Zero;
                            goto case 8;
                        }
                    case 7:
                        {
                            ((float*)Unsafe.AsPointer(ref vector))[7] = 0.0f;
                            goto case 8;
                        }
                    case 8: return Vector256.Sum(advanceVector) * currentMetrics.RelativeScale;
                    default: return 0.0f;
                }
            }
        }

        internal readonly record struct Policy : IPooledObjectPolicy<OverflowTextWrapper>
        {
            public OverflowTextWrapper Create() => new OverflowTextWrapper();
            public bool Return(OverflowTextWrapper obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
