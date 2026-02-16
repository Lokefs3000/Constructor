using CommunityToolkit.HighPerformance;
using Editor.UI.Elements;
using Primary.Common;
using Primary.Common.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.UI.Debugging
{
    public sealed class LayoutRecorder : IDisposable
    {
        private LinearBlockAllocator _allocator;
        private Dictionary<UIElement, ElementState> _states;

        private Dictionary<ElementKey, ElementValue> _temporaryValueDict;

        private List<KeyValuePair<ElementKey, ElementValue>> _changes;
        private List<KeyValuePair<UIElement, IndexRange>> _elements;

        private bool _disposedValue;

        internal LayoutRecorder()
        {
            _allocator = new LinearBlockAllocator(1024);
            _states = new Dictionary<UIElement, ElementState>();

            _temporaryValueDict = new Dictionary<ElementKey, ElementValue>();

            _changes = new List<KeyValuePair<ElementKey, ElementValue>>();
            _elements = new List<KeyValuePair<UIElement, IndexRange>>();
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

        internal void ResetInternalState()
        {
            _allocator.Reset();
            _states.Clear();

            _temporaryValueDict.Clear();

            _changes.Clear();
            _elements.Clear();
        }

        internal void CheckForChanges(UIElement element)
        {
            if (_states.TryGetValue(element, out ElementState state))
            {
                int startIndex = _changes.Count;
                foreach (var kvp in _temporaryValueDict)
                {
                    if (!state.Dict.TryGetValue(kvp.Key, out ElementValue value) || !value.ByteMatch(kvp.Value))
                    {
                        _changes.Add(new KeyValuePair<ElementKey, ElementValue>(kvp.Key, kvp.Value));
                        state.Dict[kvp.Key] = kvp.Value;
                    }
                }

                if (_changes.Count > startIndex)
                {
                    _elements.Add(new KeyValuePair<UIElement, IndexRange>(element, new IndexRange(startIndex, _changes.Count)));
                }
            }
        }

        public void AddValue<T, TValue>(T element, string name, TValue value) where T : UIElement where TValue : unmanaged
        {
            ElementKey key = new ElementKey(typeof(T), name);
            ElementValue val = AllocateValue(ref value);

            if (_states.TryGetValue(element, out ElementState state))
            {
                _temporaryValueDict[key] = val;
            }
            else
            {
                state = new ElementState(new Dictionary<ElementKey, ElementValue>());

                _temporaryValueDict[key] = val;
                _states.Add(element, state);
            }
        }

        private unsafe ElementValue AllocateValue<T>(ref readonly T value) where T : unmanaged
        {
            byte* ptr = (byte*)_allocator.Allocate(Unsafe.SizeOf<T>());
            NativeMemory.Copy(Unsafe.AsPointer(in value), ptr, (nuint)Unsafe.SizeOf<T>());

            return new ElementValue(typeof(T), (nint)ptr, Unsafe.SizeOf<T>());
        }

        private readonly record struct ElementState(Dictionary<ElementKey, ElementValue> Dict);
        private readonly record struct ElementValue(Type Type, nint Value, int ByteSize)
        {
            //TODO: speed up using SIMD instructions?
            public unsafe bool ByteMatch(ElementValue other) => new Span<byte>(Value.ToPointer(), ByteSize).SequenceEqual(new Span<byte>(other.Value.ToPointer(), ByteSize));
        }
        private readonly record struct ElementKey(Type Type, string Name)
        {
            public override int GetHashCode() => Type.GetHashCode() ^ Name.GetDjb2HashCode();
        }
    }
}
