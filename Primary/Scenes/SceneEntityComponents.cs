using Arch.Core;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Components;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Primary.Scenes
{
    //TODO: stop enumeration when EntityData is modified!
    public ref struct SceneEntityComponents : IEnumerable
    {
        private EntityData _entityData;

        public SceneEntityComponents() => throw new NotImplementedException();
        internal SceneEntityComponents(EntityData entityData)
        {
            _entityData = entityData;
        }

        [UnscopedRef]
        public Enumerator GetEnumerator() => new Enumerator(ref _entityData);
        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

        public ref struct Enumerator : IEnumerator
        {
            private EntityData _entityData;
            private Span<ComponentType> _components;
            private int _componentsLength;
            private int _index;

            public Enumerator() => throw new NotImplementedException();
            internal Enumerator(ref EntityData entityData)
            {
                Span<ComponentType> types = entityData.Archetype.Signature.Components;
                
                _entityData = entityData;
                _components = types;
                _componentsLength = types.Length;
                _index = _componentsLength - 1;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                _index--;
                return _index > 0;
            }

            public void Reset()
            {
                _index = _componentsLength - 1;
            }

            public object Current
            {
                get
                {
                    Array array = _entityData.Chunk.GetArray(_components[_index]);

                    object? obj = array.GetValue(_entityData.Slot.Index);
                    Debug.Assert(obj != null && obj is IComponent);

                    return obj;
                }
            }
        }
    }
}
