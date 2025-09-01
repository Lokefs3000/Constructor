using Arch.LowLevel;
using Primary.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Scenes
{
    [DebuggerTypeProxy(typeof(SceneEntityChildrenDebugView))]
    public readonly record struct SceneEntityChildren : IEnumerable<SceneEntity>, IReadOnlyList<SceneEntity>
    {
        private readonly EntityRelationships _relationships;
        
        internal SceneEntityChildren(EntityRelationships relationships)
        {
            _relationships = relationships;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<SceneEntity> GetEnumerator()
        {
            return new Enumerator(_relationships);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _relationships.Children.Count;
        public SceneEntity this[int index] => new SceneEntity(_relationships.Children[index]);

        public bool IsEmpty => _relationships.Children.Count == 0;

        private record struct Enumerator : IEnumerator<SceneEntity>
        {
            private readonly EntityRelationships _relationships;
            private int _index;

            internal Enumerator(EntityRelationships relationships)
            {
                _relationships = relationships;
                _index = -1;
            }

            public SceneEntity Current => new SceneEntity(_relationships.Children[_index]);
            object IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                _index++;
                return _index < _relationships.Children.Count;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                _index = 0;
            }
        }
    }

    internal class SceneEntityChildrenDebugView
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SceneEntityChildren _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SceneEntity[] Items
        {
            get
            {
                return _entity.ToArray();
            }
        }

        public SceneEntityChildrenDebugView(SceneEntityChildren entity) => _entity = entity;
    }
}
