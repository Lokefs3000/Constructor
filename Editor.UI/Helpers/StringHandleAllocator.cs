using CommunityToolkit.HighPerformance;
using Primary.Common.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Helpers
{
    internal unsafe sealed class StringHandleAllocator
    {
        private readonly LinearBlockAllocator _allocator;

        private Dictionary<int, StringHandle> _cachedHandles;

        internal StringHandleAllocator(LinearBlockAllocator allocator)
        {
            _allocator = allocator;

            _cachedHandles = new Dictionary<int, StringHandle>();
        }

        internal void Clear()
        {
            _cachedHandles.Clear();
        }

        internal StringHandle GetStringHandle(ReadOnlySpan<char> text)
        {
            if (text.Length < 200)
            {
                int djb2 = text.GetDjb2HashCode();
                if (_cachedHandles.TryGetValue(djb2, out StringHandle handle))
                    return handle;

                char* start = (char*)_allocator.Allocate((text.Length + 1) * 2);

                text.CopyTo(new Span<char>(start, text.Length));
                start[text.Length] = '\0';

                handle = new StringHandle(start, text.Length + 1, djb2);
                _cachedHandles.Add(djb2, handle);

                return handle;
            }
            else
            {
                char* start = (char*)_allocator.Allocate((text.Length + 1) * 2);

                text.CopyTo(new Span<char>(start, text.Length));
                start[text.Length] = '\0';

                return new StringHandle(start, text.Length + 1, int.MinValue);
            }
        }
    }
}
