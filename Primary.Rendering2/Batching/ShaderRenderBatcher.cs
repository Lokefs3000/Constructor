using Arch.LowLevel;
using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Batching
{
    public sealed class ShaderRenderBatcher : IDisposable
    {
        private ShaderAsset? _activeShader;

        private PooledList<RenderSegment> _segments;
        private UnsafeList<RenderFlag> _flags;

        private bool _disposedValue;

        internal ShaderRenderBatcher()
        {
            _activeShader = null;

            _segments = new PooledList<RenderSegment>();
            _flags = new UnsafeList<RenderFlag>(8);
        }

        internal void ClearFrameData(ShaderAsset shader)
        {
            _activeShader = shader;

            _segments.Clear();
            _flags.Clear();
        }

        internal void Execute(RenderList list, ReadOnlySpan<OctreeRenderBatcher> batchers)
        {
            Debug.Assert(_activeShader != null);

            _segments.Clear();
            _flags.Clear();

            uint shaderKey = list.GetShaderId(_activeShader);

            RenderKey previous;
            int previousIndex;
            UnbatchedRenderFlag previousRd;

            if (list.GetKeyRangeForShader(_activeShader, out ShaderKeyRange keyRange))
            {
                Span<RenderKey> keys = list.GetSlicedKeyRange(keyRange);

                if (keys.IsEmpty)
                    return;
                else if (keys.Length == 1)
                {
                    ref readonly RenderKey key = ref keys[0];
                    ref readonly UnbatchedRenderFlag rd = ref batchers[key.Batcher].GetFlagRef(key.Index);

                    Debug.Assert(key.ShaderId == shaderKey);

                    _segments.Add(new RenderSegment(rd.Material, rd.Mesh, 0, 1));
                    _flags.Add(new RenderFlag(rd.Model, key.MaterialId));

                    return;
                }

                //ref readonly RenderKey lastKey = ref keys[keys.Length - 1];

                {
                    ref readonly RenderKey firstKey = ref keys[keys.Length - 1];
                    Debug.Assert(firstKey.ShaderId == shaderKey);

                    previous = firstKey;
                    previousIndex = _flags.Count;
                    previousRd = batchers[firstKey.Batcher].GetFlagRef(firstKey.Index);

                    _flags.Add(new RenderFlag(previousRd.Model, firstKey.MaterialId));
                }

                RenderKey current;
                Unsafe.SkipInit(out current);

                do
                {
                    ref readonly RenderKey key = ref keys[_flags.Count];
                    Debug.Assert(!Unsafe.IsNullRef(in key));
                    Debug.Assert(key.ShaderId == shaderKey);

                    //if (key.ShaderId != shaderKey)
                    //    break;

                    ref readonly UnbatchedRenderFlag rd = ref batchers[key.Batcher].GetFlagRef(key.Index);

                    current = key;
                    if (!previous.Equals(current))
                    {
                        _segments.Add(new RenderSegment(previousRd.Material, previousRd.Mesh, previousIndex, _flags.Count));

                        previous = current;
                        previousIndex = _flags.Count;
                        previousRd = rd;
                    }

                    _flags.Add(new RenderFlag(rd.Model, key.MaterialId));
                } while (_flags.Count < keys.Length);

                if (previousIndex < _flags.Count)
                {
                    _segments.Add(new RenderSegment(previousRd.Material, previousRd.Mesh, previousIndex, _flags.Count));
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                _segments.Dispose();
                _flags.Dispose();

                _disposedValue = true;
            }
        }

        ~ShaderRenderBatcher()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ShaderAsset? ActiveShader => _activeShader;

        public ReadOnlySpan<RenderSegment> Segments => _segments.Span;
        public ReadOnlySpan<RenderFlag> Flags => _flags.AsSpan();
    }
}
