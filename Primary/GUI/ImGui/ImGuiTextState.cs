using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.GUI.ImGui
{
    public sealed class ImGuiTextState
    {
        public char[] Buffer { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }

        public int SelCharIndex { get; set; }
        public int SelCharCount { get; set; }

        internal ImGuiTextState()
        {
            Buffer = Array.Empty<char>();
            Position = 0;
            Length = 0;

            SelCharIndex = 0;
            SelCharCount = 0;
        }

        internal void ResetState()
        {
            Position = 0;
            Length = 0;

            SelCharIndex = 0;
            SelCharCount = 0;
        }

        private void MoveBuffer(int from, int to, int length = -1)
        {
            if (from == to)
            {
                return;
            }

            if (length == -1)
            {
                length = Length;
            }

            if (length == 0)
            {
                return;
            }

            Array.Copy(Buffer, from, Buffer, to, length - from);
        }

        public void ResizeToAtleast(int min)
        {
            if (min > Length)
            {
                int newSize = Math.Max(Length, 16);
                while (newSize < min)
                    newSize *= 2;

                char[] newBuffer = new char[newSize];
                Array.Copy(Buffer, newBuffer, Length);

                Buffer = newBuffer;
            }
        }

        public void Clear()
        {
            Length = 0;
            Position = 0;
        }

        public void Append(char c)
        {
            ResizeToAtleast(Length + 1);

            if (Position == Length)
            {
                Buffer[Position++] = c;
            }
            else
            {
                MoveBuffer(Position, Position + 1);
                Buffer[Position++] = c;
            }

            Length++;
        }

        public void Append(ReadOnlySpan<char> str)
        {
            if (str.IsEmpty)
            {
                return;
            }

            ResizeToAtleast(Length + str.Length);

            if (Position == Length)
            {
                str.CopyTo(Buffer.AsSpan(Position++));
            }
            else
            {
                MoveBuffer(Position, Position + str.Length);
                str.CopyTo(Buffer.AsSpan(Position++));
            }

            Length += str.Length;
        }

        public void Insert(int index, char c)
        {
            ResizeToAtleast(Length + 1);

            if (index == Length)
            {
                Buffer[index] = c;
            }
            else
            {
                MoveBuffer(index, index + 1);
                Buffer[index] = c;
            }

            Length++;
            Position++;
        }

        public void RemoveAt(int index)
        {
            if (Length <= 0 || index < 1)
            {
                return;
            }

            if (index < Length)
            {
                MoveBuffer(index, index - 1);
            }

            if (index == Position)
                Position--;
            Length--;
        }

        public void RemoveRange(int index, int count)
        {
            if (Length <= 0 || index + count > Length || index + count < 0)
            {
                return;
            }

            if (index < Length)
            {
                MoveBuffer(index + count, index);
            }

            if (index >= Position || index + count <= Position)
                Position -= count;
            Length -= count;
        }

        public override string ToString()
        {
            if (Length == 0)
                return string.Empty;

            return Buffer.AsSpan(0, Position).ToString();
        }

        public Span<char> AsSpan()
        {
            return Buffer.AsSpan(0, Position);
        }
    }
}
