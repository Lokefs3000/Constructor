using Editor.UI.Assets;
using Editor.UI.Elements;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Text
{
    public sealed class UITextShaper
    {
        internal UITextShaper()
        {

        }

        public void ShapeText(UITextShapingData shapingData, UITextShapingInfo info)
        {
            shapingData.ClearPreviousData(info.Style, info.Text);

            switch (info.Overflow)
            {
                case UITextOverflow.Overflow: ProcessTextOverflow(shapingData, ref info); break;
                case UITextOverflow.Cull: ProcessTextCull(shapingData, ref info); break;
                case UITextOverflow.WrapWords:
                case UITextOverflow.WrapLetters: ProcessTextWrap(shapingData, ref info, info.Overflow == UITextOverflow.WrapLetters); break;
            }
        }

        private void ProcessTextOverflow(UITextShapingData shapingData, ref readonly UITextShapingInfo info)
        {
            ReadOnlySpan<char> letters = info.Text.AsSpan();

            Vector2 spaceTabAdvance = new Vector2(info.Style.SpaceAdvance, info.Style.TabAdvance);
            int lastLetterIndex = letters.Length - 1;

            Vector2 size = Vector2.Zero;

            for (int i = 0; i < letters.Length; i++)
            {
                char letter = letters[i];
                switch (letter)
                {
                    case ' ': size.X += spaceTabAdvance.X; break;
                    case '\t': size.X += spaceTabAdvance.Y; break;
                    default:
                        {
                            UIGlyph glyph = info.Style.RequestGlyph(letter);

                            if (i == lastLetterIndex)
                            {
                                size.X += MathF.Max(glyph.Offset.X + glyph.Size.X, 0.0f);
                            }
                            else
                            {
                                size.X += glyph.Advance;
                            }

                            size.Y = MathF.Max(size.Y, glyph.Offset.Y + glyph.Size.Y);
                            break;
                        }
                }
            }

            if (size.X > 0.0f)
            {
                size *= info.Size;

                shapingData.SetTotalSize(size);
                shapingData.AddLine(size, new IndexRange(0, letters.Length));
            }
        }

        private void ProcessTextCull(UITextShapingData shapingData, ref readonly UITextShapingInfo info)
        {
            ReadOnlySpan<char> letters = info.Text.AsSpan();

            Vector2 spaceTabAdvance = new Vector2(info.Style.SpaceAdvance, info.Style.TabAdvance);
            Vector2 relativeExtents = info.MaxExtents / info.Size;
            int lastLetterIndex = letters.Length - 1;

            Vector2 size = Vector2.Zero;
            int letterCount = -1;

            for (int i = 0; i < letters.Length; i++)
            {
                char letter = letters[i];

                Vector2 prevSize = size;

                switch (letter)
                {
                    case ' ': size.X += spaceTabAdvance.X; break;
                    case '\t': size.X += spaceTabAdvance.Y; break;
                    default:
                        {
                            UIGlyph glyph = info.Style.RequestGlyph(letter);

                            if (i == lastLetterIndex)
                            {
                                size.X += MathF.Max(glyph.Offset.X + glyph.Size.X, 0.0f);
                            }
                            else
                            {
                                size.X += glyph.Advance;
                            }

                            size.Y = MathF.Max(size.Y, glyph.Offset.Y + glyph.Size.Y);
                            break;
                        }
                }

                if (size.X > relativeExtents.X)
                {
                    size = prevSize;
                    letterCount = i;
                    break;
                }
            }

            if (size.X > 0.0f)
            {
                size *= info.Size;

                shapingData.SetTotalSize(size);
                shapingData.AddLine(size, new IndexRange(0, letterCount == -1 ? letters.Length : letterCount));
            }
        }

        private void ProcessTextWrap(UITextShapingData shapingData, ref readonly UITextShapingInfo info, bool wrapLetters)
        {
            ReadOnlySpan<char> letters = info.Text.AsSpan();

            Vector2 spaceTabAdvance = new Vector2(info.Style.SpaceAdvance, info.Style.TabAdvance);
            Vector2 relativeExtents = info.MaxExtents / info.Size;
            int lastLetterIndex = letters.Length - 1;

            Vector2 size = Vector2.Zero;

            float lineHeight = info.LineHeight / info.Size;
            float lineIndex = 0.0f;

            Vector2 largestSize = Vector2.Zero;

            Vector2 whitespaceCheckpoint = -Vector2.One;
            Vector2 letterCheckpoint = -Vector2.One;

            int prevWhitespaceIndex = 0;
            int prevLetterStartIndex = 0;

            for (int i = 0; i < letters.Length; i++)
            {
                char letter = letters[i];

                float glyphWidth = 0.0f;
                float glyphHeight = 0.0f;

                switch (letter)
                {
                    case ' ': glyphWidth = spaceTabAdvance.X; break;
                    case '\t': glyphWidth = spaceTabAdvance.Y; break;
                    default:
                        {
                            UIGlyph glyph = info.Style.RequestGlyph(letter);

                            if (i == lastLetterIndex)
                            {
                                glyphWidth = MathF.Max(glyph.Offset.X + glyph.Size.X, 0.0f);
                            }
                            else
                            {
                                glyphWidth = glyph.Advance;
                            }

                            glyphHeight = MathF.Max(size.Y, glyph.Offset.Y + glyph.Size.Y);
                            break;
                        }
                }

                float sizeNext = size.X + glyphWidth;
                if (sizeNext > relativeExtents.X)
                {
                    if (size.X > 0.0f)
                    {
                        if (char.IsWhiteSpace(letter))
                        {
                            shapingData.AddLine(letterCheckpoint * info.Size, new IndexRange(prevLetterStartIndex, i));
                            size = new Vector2(glyphWidth, 0.0f);
                        }
                        else
                        {
                            if (wrapLetters)
                            {
                                shapingData.AddLine(size * info.Size, new IndexRange(prevLetterStartIndex, i));
                                size = new Vector2(glyphWidth, 0.0f);
                            }
                            else
                            {
                                //no whitespace in line so fall back to wrapping letters instead
                                if (whitespaceCheckpoint.X < 0.0f)
                                {
                                    shapingData.AddLine(size * info.Size, new IndexRange(prevLetterStartIndex, i));
                                    size = new Vector2(glyphWidth, 0.0f);
                                }
                                else
                                {
                                    shapingData.AddLine(whitespaceCheckpoint * info.Size, new IndexRange(prevLetterStartIndex, prevWhitespaceIndex));
                                    size = Vector2.Zero;

                                    i = prevWhitespaceIndex + 1;
                                }
                            }
                        }
                    }

                    whitespaceCheckpoint = -Vector2.One;
                    letterCheckpoint = -Vector2.One;
                    prevLetterStartIndex = i;

                    lineIndex += lineHeight;
                }
                else
                {
                    size.X = sizeNext;

                    if (char.IsWhiteSpace(letter))
                    {
                        whitespaceCheckpoint = size;
                        prevWhitespaceIndex = i;
                    }
                    else
                    {
                        letterCheckpoint = size;
                    }
                }

                size.Y = MathF.Max(size.Y, glyphHeight);
                largestSize = Vector2.Max(largestSize, new Vector2(size.X, size.Y + lineIndex));
            }

            if (size.X > 0.0f)
                shapingData.AddLine(size * info.Size, new IndexRange(prevLetterStartIndex, letters.Length));
        }
    }
}
