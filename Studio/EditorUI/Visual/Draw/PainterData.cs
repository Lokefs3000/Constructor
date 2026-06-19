using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Common.Memory;

namespace EditorUI.Visual.Draw
{
    public sealed class PainterData : IDisposable
    {
        private LinearBlockAllocator _allocator;

        private List<object> _objects;
        private Dictionary<object, int> _objectsIdDict;

        private bool _disposedValue;

        internal PainterData()
        {
            _allocator = new LinearBlockAllocator(1024);

            _objects = new List<object>();
            _objectsIdDict = new Dictionary<object, int>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _allocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearForNewPaint()
        {
            _allocator.Reset();
            _objects.Clear();
            _objectsIdDict.Clear();
        }

        internal unsafe Span<byte> AllocateSpace(int space)
        {
            return new Span<byte>(_allocator.Allocate(space).ToPointer(), space);
        }

        internal int GetObjectIndex(object obj)
        {
            ref int index = ref CollectionsMarshal.GetValueRefOrAddDefault(_objectsIdDict, obj, out bool exists);
            if (exists)
                return index;

            index = _objects.Count;
            _objects.Add(obj);

            return index;
        }

        internal unsafe ReadOnlySpan<byte> Memory => new ReadOnlySpan<byte>(_allocator.Pointer.ToPointer(), _allocator.CurrentOffset);
        internal ROList<object> Objects => _objects;

        public bool IsEmpty => _allocator.CurrentOffset == 0;
    }
}
