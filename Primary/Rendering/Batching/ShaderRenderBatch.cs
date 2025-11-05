using Arch.LowLevel;
using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common.Native;
using Primary.Pooling;
using Primary.Rendering.Data;
using SharpGen.Runtime;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Batching
{
    public unsafe sealed class ShaderRenderBatch : IDisposable
    {
        private ShaderAsset? _shaderReference;

        private PooledList<BatchedSegment> _segments;
        private UnsafeList<BatchedRenderFlag> _renderFlags;

        private bool _disposedValue;

        internal ShaderRenderBatch()
        {
            _shaderReference = null;

            _segments = new PooledList<BatchedSegment>();
            _renderFlags = new UnsafeList<BatchedRenderFlag>(8);
        }

        internal void ResetForNextFrame(ShaderAsset shader)
        {
            _shaderReference = shader;

            _segments.TrimExcess();
            _segments.Clear();

            _renderFlags.Clear(); //TODO: add shrinking support to array
        }

        internal void ClearData()
        {
            _shaderReference = null;

            _segments.TrimExcess();
            _segments.Clear();

            _renderFlags.Clear();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                ClearData();

                _disposedValue = true;
            }
        }

        ~ShaderRenderBatch()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Execute(RenderBatcher batcher, ReadOnlySpan<RenderTreeSubCollector> collectors)
        {
            Debug.Assert(_shaderReference != null);

            _segments.Clear();
            _segments.TrimExcess();

            _renderFlags.Clear();

            uint shaderKey = _shaderReference!.Id;

            Vector128<uint> previous;
            int previousIndex = 0;
            UnbatchedRenderData previousRd = default;

            //Unsafe.SkipInit(out previous);
            previous = Vector128<uint>.Zero;

            if (batcher.GetBaseIndexForShader(_shaderReference!, out ShaderKeyRange keyRange))
            {
                Span<RenderKey> keys = batcher.GetKeyRange(keyRange);

                if (keys.IsEmpty)
                    return;
                else if (keys.Length == 1)
                {
                    RenderKey key = keys[0];
                    ref readonly UnbatchedRenderData rd = ref collectors[key.CollectorIndex].GetEntityRef(key.ListIndex);

                    _renderFlags.Add(new BatchedRenderFlag(rd.Model, key.MaterialId));
                    _segments.Add(new BatchedSegment(rd.Material, rd.Mesh, 0, 1));

                    return;
                }

                ref RenderKey lastKey = ref keys[keys.Length - 1];

                {
                    ref readonly RenderKey firstKey = ref keys[0];
                    Debug.Assert(firstKey.ShaderId == shaderKey);

                    //Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<uint>, byte>(ref previous), firstKey);
                    previous = Vector128.Create(firstKey.ShaderId, firstKey.ModelId, firstKey.MaterialId, firstKey.MeshId);
                    previousIndex = _renderFlags.Count;
                    previousRd = collectors[firstKey.CollectorIndex].GetEntityRef(firstKey.ListIndex);

                    _renderFlags.Add(new BatchedRenderFlag(previousRd.Model, firstKey.MaterialId));
                }

                Vector128<uint> current;
                Unsafe.SkipInit(out current);

                do
                {
                    ref readonly RenderKey key = ref keys[_renderFlags.Count];
                    Debug.Assert(!Unsafe.IsNullRef(in key));

                    if (key.ShaderId != shaderKey)
                        break;

                    ref readonly UnbatchedRenderData rd = ref collectors[key.CollectorIndex].GetEntityRef(key.ListIndex);

                    current = Vector128.Create(key.ShaderId, key.ModelId, key.MaterialId, key.MeshId);
                    if (!Vector128.EqualsAll(previous, current))
                    {
                        _segments.Add(new BatchedSegment(previousRd.Material, previousRd.Mesh, previousIndex, _renderFlags.Count));
                    
                        previous = current;
                        previousIndex = _renderFlags.Count;
                        previousRd = rd;
                    }

                    _renderFlags.Add(new BatchedRenderFlag(rd.Model, key.MaterialId));
                }
                while (_renderFlags.Count < keys.Length);

                if (previousIndex < _renderFlags.Count)
                {
                    _segments.Add(new BatchedSegment(previousRd.Material, previousRd.Mesh, previousIndex, _renderFlags.Count));
                }
            }
        }

        internal ShaderAsset? ShaderReference => _shaderReference;

        internal Span<BatchedSegment> Segments => _segments.Span;
        internal Span<BatchedRenderFlag> RenderFlags => _renderFlags.AsSpan();

        internal struct PoolingPolicy : IObjectPoolPolicy<ShaderRenderBatch>
        {
            public ShaderRenderBatch Create() => new ShaderRenderBatch();

            public bool Return(ref ShaderRenderBatch obj)
            {
                obj.ClearData();
                return true;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly record struct BatchedRenderFlag(Matrix4x4 Model, uint MaterialId);

    internal readonly record struct BatchedSegment(MaterialAsset Material, RawRenderMesh Mesh, int IdxStart, int IdxEnd);
}
