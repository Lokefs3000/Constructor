using Arch.LowLevel;
using Editor.Memory;
using Primary.Common;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.Rendering.Gui
{
    public sealed class GuiCommandBuffer : IDisposable
    {
        private readonly LinearAllocator _allocator;

        private bool _disposedValue;

        private UnsafeList<GuiDraw> _draws;

        internal GuiCommandBuffer(LinearAllocator allocator)
        {
            _allocator = allocator;

            _draws = new UnsafeList<GuiDraw>(64);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            _draws.Clear();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _draws.Dispose();

                _disposedValue = true;
            }
        }

        ~GuiCommandBuffer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawSolidRect(Vector2 minimum, Vector2 maximum, Color color)
        {
            _draws.Add(new GuiDraw
            {
                Type = GuiDrawType.SolidRect,
                SolidRect = new GuiDrawSolidRect
                {
                    Minimum = minimum,
                    Maximum = maximum,
                    Color = color.AsVector4()
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRegularText(Vector2 cursor, ReadOnlySpan<char> text, float size, Color color)
        {
            _draws.Add(new GuiDraw
            {
                Type = GuiDrawType.RegularText,
                RegularText = new GuiDrawRegularText
                {
                    Cursor = cursor,
                    Text = new StringBuffer(_allocator, text),
                    Scale = size,
                    Color = color.AsVector4()
                }
            });
        }

        internal Span<GuiDraw> Draws => _draws.AsSpan();
    }

    internal enum GuiDrawType : byte
    {
        Unknown = 0,
        SolidRect,
        RegularText
    }

    [StructLayout(LayoutKind.Explicit)]
    internal record struct GuiDraw
    {
        [FieldOffset(0)]
        public GuiDrawType Type;

        [FieldOffset(1)]
        private ushort ZIndex;

        [FieldOffset(3)]
        public GuiDrawSolidRect SolidRect;

        [FieldOffset(3)]
        public GuiDrawRegularText RegularText;
    }

    internal record struct GuiDrawSolidRect
    {
        public Vector2 Minimum;
        public Vector2 Maximum;

        public Vector4 Color;
    }

    internal record struct GuiDrawRegularText
    {
        public Vector2 Cursor;
        public StringBuffer Text;

        public float Scale;

        public Vector4 Color;
    }

    internal unsafe struct StringBuffer
    {
        private char* _buffer;
        private int _length;

        public StringBuffer(char* buffer, int length)
        {
            _buffer = buffer;
            _length = length;
        }

        public StringBuffer(nint buffer, int length)
        {
            _buffer = (char*)buffer;
            _length = length;
        }

        public StringBuffer(LinearAllocator allocator, ReadOnlySpan<char> text)
        {
            _buffer = (char*)allocator.Allocate((uint)(text.Length + text.Length + 2));
            _length = text.Length;

            Span<char> span = AsSpan();
            text.CopyTo(span);
        }

        public Span<char> AsSpan() => new Span<char>(_buffer, _length);
    }
}
